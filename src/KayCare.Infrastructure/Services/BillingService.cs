using KayCare.Core.Constants;
using KayCare.Core.DTOs.Billing;
using KayCare.Core.Entities;
using KayCare.Core.Exceptions;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KayCare.Infrastructure.Services;

public class BillingService : IBillingService
{
    private readonly AppDbContext        _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext      _tenantContext;

    public BillingService(AppDbContext db, ICurrentUserService currentUser, ITenantContext tenantContext)
    {
        _db          = db;
        _currentUser = currentUser;
        _tenantContext = tenantContext;
    }

    // ── Create ────────────────────────────────────────────────────────────────

    public async Task<BillDetailResponse> CreateAsync(CreateBillRequest req, CancellationToken ct = default)
    {
        var patientExists = await _db.Patients.AnyAsync(p => p.PatientId == req.PatientId, ct);
        if (!patientExists) throw new NotFoundException(nameof(Patient), req.PatientId);

        if (!req.Items.Any())
            throw new AppException("A bill must contain at least one item.", 400);

        foreach (var item in req.Items)
        {
            if (item.Quantity <= 0)
                throw new AppException("Item quantity must be greater than zero.", 400);
            if (item.UnitPrice < 0)
                throw new AppException("Item unit price cannot be negative.", 400);
        }

        var billNumber = await GenerateBillNumberAsync(ct);

        if (req.PayerId.HasValue)
        {
            var payerExists = await _db.Payers.AnyAsync(p => p.PayerId == req.PayerId.Value, ct);
            if (!payerExists) throw new NotFoundException(nameof(Payer), req.PayerId.Value);
        }

        var bill = new Bill
        {
            BillNumber      = billNumber,
            PatientId       = req.PatientId,
            ConsultationId  = req.ConsultationId,
            PayerId         = req.PayerId,
            CreatedByUserId = _currentUser.UserId,
            Status          = BillStatus.Draft,
            Notes           = req.Notes,
            DiscountAmount  = req.DiscountAmount,
            DiscountReason  = req.DiscountReason,
            TotalAmount     = 0m,
            PaidAmount      = 0m
        };

        _db.Bills.Add(bill);
        await _db.SaveChangesAsync(ct); // flush to get BillId

        var items = req.Items.Select(i => new BillItem
        {
            BillId      = bill.BillId,
            TenantId    = _tenantContext.TenantId,
            Description = i.Description.Trim(),
            Category    = i.Category?.Trim(),
            Quantity    = i.Quantity,
            UnitPrice   = i.UnitPrice
        }).ToList();

        _db.BillItems.AddRange(items);
        await _db.SaveChangesAsync(ct);

        // Update TotalAmount from computed TotalPrice values
        var total = await _db.BillItems
            .Where(i => i.BillId == bill.BillId)
            .SumAsync(i => i.TotalPrice, ct);

        bill.TotalAmount = total;
        await _db.SaveChangesAsync(ct);

        return await LoadDetailAsync(bill.BillId, ct);
    }

    // ── Read ──────────────────────────────────────────────────────────────────

    public async Task<BillDetailResponse> GetByIdAsync(Guid billId, CancellationToken ct = default)
        => await LoadDetailAsync(billId, ct);

    public async Task<IReadOnlyList<BillResponse>> GetPatientBillsAsync(Guid patientId, CancellationToken ct = default)
    {
        var patientExists = await _db.Patients.AnyAsync(p => p.PatientId == patientId, ct);
        if (!patientExists) throw new NotFoundException(nameof(Patient), patientId);

        var rows = await _db.Bills
            .Include(b => b.Patient)
            .AsNoTracking()
            .Where(b => b.PatientId == patientId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(ct);

        return rows.Select(MapToSummary).ToList();
    }

    public async Task<IReadOnlyList<BillResponse>> GetOutstandingAsync(CancellationToken ct = default)
    {
        var rows = await _db.Bills
            .Include(b => b.Patient)
            .AsNoTracking()
            .Where(b => b.Status == BillStatus.Issued || b.Status == BillStatus.PartiallyPaid)
            .OrderBy(b => b.IssuedAt)
            .ThenBy(b => b.CreatedAt)
            .ToListAsync(ct);

        return rows.Select(MapToSummary).ToList();
    }

    // ── Issue ─────────────────────────────────────────────────────────────────

    public async Task<BillDetailResponse> IssueAsync(Guid billId, CancellationToken ct = default)
    {
        var bill = await _db.Bills
            .FirstOrDefaultAsync(b => b.BillId == billId, ct)
            ?? throw new NotFoundException(nameof(Bill), billId);

        if (bill.Status != BillStatus.Draft)
            throw new AppException($"Cannot issue a bill with status '{bill.Status}'.", 409);

        bill.Status   = BillStatus.Issued;
        bill.IssuedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return await LoadDetailAsync(billId, ct);
    }

    // ── Payment ───────────────────────────────────────────────────────────────

    public async Task<BillDetailResponse> AddPaymentAsync(Guid billId, AddPaymentRequest req, CancellationToken ct = default)
    {
        var bill = await _db.Bills
            .FirstOrDefaultAsync(b => b.BillId == billId, ct)
            ?? throw new NotFoundException(nameof(Bill), billId);

        if (bill.Status != BillStatus.Issued && bill.Status != BillStatus.PartiallyPaid)
            throw new AppException($"Cannot add a payment to a bill with status '{bill.Status}'.", 400);

        if (req.Amount <= 0)
            throw new AppException("Payment amount must be greater than zero.", 400);

        if (req.Amount > bill.BalanceDue)
            throw new AppException($"Payment amount ({req.Amount:F2}) exceeds balance due ({bill.BalanceDue:F2}).", 400);

        var payment = new Payment
        {
            BillId           = billId,
            Amount           = req.Amount,
            PaymentMethod    = req.PaymentMethod.Trim(),
            Reference        = req.Reference?.Trim(),
            ReceivedByUserId = _currentUser.UserId,
            PaymentDate      = DateTime.UtcNow,
            Notes            = req.Notes?.Trim()
        };

        _db.Payments.Add(payment);

        bill.PaidAmount += req.Amount;

        bill.Status = bill.PaidAmount >= (bill.TotalAmount - bill.DiscountAmount)
            ? BillStatus.Paid
            : BillStatus.PartiallyPaid;

        await _db.SaveChangesAsync(ct);

        return await LoadDetailAsync(billId, ct);
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    public async Task<BillDetailResponse> CancelAsync(Guid billId, CancellationToken ct = default)
    {
        var bill = await _db.Bills
            .FirstOrDefaultAsync(b => b.BillId == billId, ct)
            ?? throw new NotFoundException(nameof(Bill), billId);

        if (bill.Status != BillStatus.Draft && bill.Status != BillStatus.Issued)
            throw new AppException($"Cannot cancel a bill with status '{bill.Status}'.", 409);

        bill.Status = BillStatus.Cancelled;
        await _db.SaveChangesAsync(ct);

        return await LoadDetailAsync(billId, ct);
    }

    // ── Void ──────────────────────────────────────────────────────────────────

    public async Task<BillDetailResponse> VoidAsync(Guid billId, CancellationToken ct = default)
    {
        var bill = await _db.Bills
            .FirstOrDefaultAsync(b => b.BillId == billId, ct)
            ?? throw new NotFoundException(nameof(Bill), billId);

        if (bill.Status != BillStatus.Paid && bill.Status != BillStatus.PartiallyPaid)
            throw new AppException($"Cannot void a bill with status '{bill.Status}'.", 400);

        bill.Status = BillStatus.Void;
        await _db.SaveChangesAsync(ct);

        return await LoadDetailAsync(billId, ct);
    }

    // ── INV Number Generation ─────────────────────────────────────────────────

    private async Task<string> GenerateBillNumberAsync(CancellationToken ct)
    {
        var year   = DateTime.UtcNow.Year;
        var prefix = $"INV-{year}-";

        var lastNumber = await _db.Bills
            .Where(b => b.BillNumber.StartsWith(prefix))
            .OrderByDescending(b => b.BillNumber)
            .Select(b => b.BillNumber)
            .FirstOrDefaultAsync(ct);

        var seq = 1;
        if (lastNumber is not null &&
            int.TryParse(lastNumber[prefix.Length..], out var last))
        {
            seq = last + 1;
        }

        return $"{prefix}{seq:D5}";
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<BillDetailResponse> LoadDetailAsync(Guid billId, CancellationToken ct)
    {
        var b = await _db.Bills
            .Include(b => b.Patient)
            .Include(b => b.CreatedBy)
            .Include(b => b.Payer)
            .Include(b => b.Items)
            .Include(b => b.Payments)
                .ThenInclude(p => p.ReceivedBy)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.BillId == billId, ct)
            ?? throw new NotFoundException(nameof(Bill), billId);

        return MapToDetail(b);
    }

    private static BillResponse MapToSummary(Bill b) => new()
    {
        BillId              = b.BillId,
        BillNumber          = b.BillNumber,
        PatientId           = b.PatientId,
        PatientName         = $"{b.Patient.FirstName} {b.Patient.LastName}".Trim(),
        MedicalRecordNumber = b.Patient.MedicalRecordNumber,
        Status              = b.Status,
        TotalAmount         = b.TotalAmount,
        DiscountAmount      = b.DiscountAmount,
        PaidAmount          = b.PaidAmount,
        BalanceDue          = b.BalanceDue,
        IssuedAt            = b.IssuedAt,
        CreatedAt           = b.CreatedAt
    };

    private static BillDetailResponse MapToDetail(Bill b) => new()
    {
        BillId              = b.BillId,
        BillNumber          = b.BillNumber,
        PatientId           = b.PatientId,
        PatientName         = $"{b.Patient.FirstName} {b.Patient.LastName}".Trim(),
        MedicalRecordNumber = b.Patient.MedicalRecordNumber,
        Status              = b.Status,
        TotalAmount         = b.TotalAmount,
        DiscountAmount      = b.DiscountAmount,
        PaidAmount          = b.PaidAmount,
        BalanceDue          = b.BalanceDue,
        IssuedAt            = b.IssuedAt,
        CreatedAt           = b.CreatedAt,
        ConsultationId      = b.ConsultationId,
        PayerId             = b.PayerId,
        PayerName           = b.Payer?.Name,
        DiscountReason      = b.DiscountReason,
        CreatedByName       = $"{b.CreatedBy.FirstName} {b.CreatedBy.LastName}".Trim(),
        Notes               = b.Notes,
        UpdatedAt           = b.UpdatedAt,
        Items = b.Items.Select(i => new BillItemResponse
        {
            ItemId      = i.ItemId,
            Description = i.Description,
            Category    = i.Category,
            Quantity    = i.Quantity,
            UnitPrice   = i.UnitPrice,
            TotalPrice  = i.TotalPrice,
            SourceType  = i.SourceType,
            SourceId    = i.SourceId
        }).ToList(),
        Payments = b.Payments.Select(p => new PaymentResponse
        {
            PaymentId      = p.PaymentId,
            Amount         = p.Amount,
            PaymentMethod  = p.PaymentMethod,
            Reference      = p.Reference,
            ReceivedByName = $"{p.ReceivedBy.FirstName} {p.ReceivedBy.LastName}".Trim(),
            PaymentDate    = p.PaymentDate,
            Notes          = p.Notes,
            CreatedAt      = p.CreatedAt
        }).ToList()
    };
}
