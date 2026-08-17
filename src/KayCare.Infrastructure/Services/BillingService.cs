using KayCare.Core.Constants;
using KayCare.Core.DTOs.Billing;
using KayCare.Core.Entities;
using KayCare.Core.Exceptions;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KayCare.Infrastructure.Services;

public class BillingService : IBillingService
{
    private readonly AppDbContext        _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext      _tenantContext;
    private readonly IAuditService       _audit;
    private readonly ILogger<BillingService> _logger;

    public BillingService(
        AppDbContext db,
        ICurrentUserService currentUser,
        ITenantContext tenantContext,
        IAuditService audit,
        ILogger<BillingService> logger)
    {
        _db          = db;
        _currentUser = currentUser;
        _tenantContext = tenantContext;
        _audit       = audit;
        _logger      = logger;
    }

    // ── Create ────────────────────────────────────────────────────────────────

    public async Task<BillDetailResponse> CreateAsync(CreateBillRequest req, CancellationToken ct = default)
    {
        using var transaction = await _db.Database.BeginTransactionAsync(ct);
        await _db.AcquireAdvisoryLockAsync(_tenantContext.TenantId, "BillNumber", ct);

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

        // If a submitted line item matches an active catalog service, re-derive its price from
        // the catalog/payer tariff server-side rather than trusting the client-submitted amount —
        // a manually-editable price field must never be the sole authority on what gets billed.
        // Lines with no catalog match (genuinely ad-hoc/manual charges) keep the submitted price.
        var catalog = await _db.ServiceCatalogItems.Where(s => s.IsActive).AsNoTracking().ToListAsync(ct);
        var items = new List<BillItem>();
        foreach (var i in req.Items)
        {
            var matched = catalog.FirstOrDefault(c => c.Name.Equals(i.Description.Trim(), StringComparison.OrdinalIgnoreCase));
            var unitPrice = matched is not null
                ? await _db.ResolveTariffOrCatalogPriceAsync(matched, bill.PayerId, ct)
                : i.UnitPrice;

            items.Add(new BillItem
            {
                BillId      = bill.BillId,
                TenantId    = _tenantContext.TenantId,
                Description = i.Description.Trim(),
                Category    = i.Category?.Trim(),
                Quantity    = i.Quantity,
                UnitPrice   = unitPrice
            });
        }

        _db.BillItems.AddRange(items);
        await _db.SaveChangesAsync(ct);

        // Update TotalAmount from computed TotalPrice values
        var total = await _db.BillItems
            .Where(i => i.BillId == bill.BillId)
            .SumAsync(i => i.TotalPrice, ct);

        bill.TotalAmount = total;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditActions.BillCreate, nameof(Bill), bill.BillId, bill.PatientId,
            details: $"BillNumber={bill.BillNumber}; Total={bill.TotalAmount:F2}", ct: ct);
        _logger.LogInformation("Bill {BillId} ({BillNumber}) created for patient {PatientId}, total {TotalAmount:F2}",
            bill.BillId, bill.BillNumber, bill.PatientId, bill.TotalAmount);

        await transaction.CommitAsync(ct);

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
        using var transaction = await _db.Database.BeginTransactionAsync(ct);
        await _db.AcquireAdvisoryLockAsync(_tenantContext.TenantId, $"Bill_{billId}", ct);

        var bill = await _db.Bills
            .FirstOrDefaultAsync(b => b.BillId == billId, ct)
            ?? throw new NotFoundException(nameof(Bill), billId);

        if (bill.Status != BillStatus.Draft)
            throw new AppException($"Cannot issue a bill with status '{bill.Status}'.", 409);

        bill.Status   = BillStatus.Issued;
        bill.IssuedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditActions.BillIssue, nameof(Bill), billId, bill.PatientId, ct: ct);
        _logger.LogInformation("Bill {BillId} issued", billId);

        await transaction.CommitAsync(ct);

        return await LoadDetailAsync(billId, ct);
    }

    // ── Payment ───────────────────────────────────────────────────────────────

    public async Task<BillDetailResponse> AddPaymentAsync(Guid billId, AddPaymentRequest req, CancellationToken ct = default)
    {
        using var transaction = await _db.Database.BeginTransactionAsync(ct);
        await _db.AcquireAdvisoryLockAsync(_tenantContext.TenantId, $"Bill_{billId}", ct);

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

        var effectiveBalance = bill.TotalAmount + bill.AdjustmentTotal - bill.DiscountAmount - bill.WriteOffAmount - bill.CreditNoteTotal - bill.PaidAmount;
        bill.Status = effectiveBalance <= 0
            ? BillStatus.Paid
            : BillStatus.PartiallyPaid;

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditActions.BillPayment, nameof(Bill), billId, bill.PatientId,
            details: $"PaymentId={payment.PaymentId}; Amount={payment.Amount:F2}; Method={payment.PaymentMethod}", ct: ct);
        _logger.LogInformation("Payment {PaymentId} of {Amount:F2} recorded against bill {BillId}, new status {Status}",
            payment.PaymentId, payment.Amount, billId, bill.Status);

        await transaction.CommitAsync(ct);

        return await LoadDetailAsync(billId, ct);
    }

    // ── Discount / Waiver ─────────────────────────────────────────────────────

    public async Task<BillDetailResponse> ApplyDiscountAsync(Guid billId, ApplyDiscountRequest req, CancellationToken ct = default)
    {
        using var transaction = await _db.Database.BeginTransactionAsync(ct);
        await _db.AcquireAdvisoryLockAsync(_tenantContext.TenantId, $"Bill_{billId}", ct);

        var bill = await _db.Bills
            .FirstOrDefaultAsync(b => b.BillId == billId, ct)
            ?? throw new NotFoundException(nameof(Bill), billId);

        if (bill.Status != BillStatus.Draft && bill.Status != BillStatus.Issued)
            throw new AppException($"Cannot apply a discount to a bill with status '{bill.Status}'.", 409);

        if (req.DiscountAmount > bill.TotalAmount)
            throw new AppException($"Discount ({req.DiscountAmount:F2}) cannot exceed the bill total ({bill.TotalAmount:F2}).", 400);

        if (req.DiscountAmount < bill.PaidAmount)
            throw new AppException($"Discount ({req.DiscountAmount:F2}) cannot reduce the balance below zero — {bill.PaidAmount:F2} has already been paid.", 400);

        bill.DiscountAmount = req.DiscountAmount;
        bill.DiscountReason = req.DiscountReason?.Trim();
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditActions.BillDiscount, nameof(Bill), billId, bill.PatientId,
            details: $"DiscountAmount={bill.DiscountAmount:F2}; Reason={bill.DiscountReason}", ct: ct);
        _logger.LogInformation("Discount of {DiscountAmount:F2} applied to bill {BillId}", bill.DiscountAmount, billId);

        await transaction.CommitAsync(ct);

        return await LoadDetailAsync(billId, ct);
    }

    // ── Adjustment ───────────────────────────────────────────────────────────

    public async Task<BillDetailResponse> AddAdjustmentAsync(Guid billId, AddAdjustmentRequest req, CancellationToken ct = default)
    {
        using var transaction = await _db.Database.BeginTransactionAsync(ct);
        await _db.AcquireAdvisoryLockAsync(_tenantContext.TenantId, $"Bill_{billId}", ct);

        var bill = await _db.Bills
            .FirstOrDefaultAsync(b => b.BillId == billId, ct)
            ?? throw new NotFoundException(nameof(Bill), billId);

        if (bill.Status != BillStatus.Issued && bill.Status != BillStatus.PartiallyPaid)
            throw new AppException($"Cannot adjust a bill with status '{bill.Status}'.", 409);

        if (req.Amount == 0)
            throw new AppException("Adjustment amount cannot be zero.", 400);

        // A credit adjustment cannot push the bill total below what's already been paid
        var newTotal = bill.TotalAmount + bill.AdjustmentTotal + req.Amount;
        if (newTotal - bill.DiscountAmount - bill.WriteOffAmount < bill.PaidAmount)
            throw new AppException("Adjustment would make the balance negative.", 400);

        var adjustment = new BillAdjustment
        {
            BillId           = billId,
            TenantId         = _tenantContext.TenantId,
            Amount           = req.Amount,
            Reason           = req.Reason.Trim(),
            AdjustedByUserId = _currentUser.UserId,
            AdjustedAt       = DateTime.UtcNow
        };

        _db.BillAdjustments.Add(adjustment);

        bill.AdjustmentTotal += req.Amount;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditActions.BillAdjustment, nameof(Bill), billId, bill.PatientId,
            details: $"AdjustmentId={adjustment.BillAdjustmentId}; Amount={adjustment.Amount:F2}; Reason={adjustment.Reason}", ct: ct);
        _logger.LogInformation("Adjustment of {Amount:F2} applied to bill {BillId}: {Reason}",
            adjustment.Amount, billId, adjustment.Reason);

        await transaction.CommitAsync(ct);

        return await LoadDetailAsync(billId, ct);
    }

    // ── Write-off ─────────────────────────────────────────────────────────────

    public async Task<BillDetailResponse> WriteOffAsync(Guid billId, WriteOffRequest req, CancellationToken ct = default)
    {
        using var transaction = await _db.Database.BeginTransactionAsync(ct);
        await _db.AcquireAdvisoryLockAsync(_tenantContext.TenantId, $"Bill_{billId}", ct);

        var bill = await _db.Bills
            .FirstOrDefaultAsync(b => b.BillId == billId, ct)
            ?? throw new NotFoundException(nameof(Bill), billId);

        if (bill.Status != BillStatus.Issued && bill.Status != BillStatus.PartiallyPaid)
            throw new AppException($"Cannot write off a bill with status '{bill.Status}'.", 409);

        if (bill.BalanceDue <= 0)
            throw new AppException("This bill has no outstanding balance to write off.", 400);

        bill.WriteOffAmount = bill.BalanceDue;   // write off the entire remaining balance
        bill.WriteOffReason = req.Reason.Trim();
        bill.Status         = BillStatus.WrittenOff;

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditActions.BillWriteOff, nameof(Bill), billId, bill.PatientId,
            details: $"WriteOffAmount={bill.WriteOffAmount:F2}; Reason={bill.WriteOffReason}", ct: ct);
        _logger.LogWarning("Bill {BillId} written off for {WriteOffAmount:F2}: {Reason}",
            billId, bill.WriteOffAmount, bill.WriteOffReason);

        await transaction.CommitAsync(ct);

        return await LoadDetailAsync(billId, ct);
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    public async Task<BillDetailResponse> CancelAsync(Guid billId, CancellationToken ct = default)
    {
        using var transaction = await _db.Database.BeginTransactionAsync(ct);
        await _db.AcquireAdvisoryLockAsync(_tenantContext.TenantId, $"Bill_{billId}", ct);

        var bill = await _db.Bills
            .FirstOrDefaultAsync(b => b.BillId == billId, ct)
            ?? throw new NotFoundException(nameof(Bill), billId);

        if (bill.Status != BillStatus.Draft && bill.Status != BillStatus.Issued)
            throw new AppException($"Cannot cancel a bill with status '{bill.Status}'.", 409);

        bill.Status = BillStatus.Cancelled;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditActions.BillCancel, nameof(Bill), billId, bill.PatientId, ct: ct);
        _logger.LogWarning("Bill {BillId} cancelled", billId);

        await transaction.CommitAsync(ct);

        return await LoadDetailAsync(billId, ct);
    }

    // ── Void ──────────────────────────────────────────────────────────────────

    public async Task<BillDetailResponse> VoidAsync(Guid billId, CancellationToken ct = default)
    {
        using var transaction = await _db.Database.BeginTransactionAsync(ct);
        await _db.AcquireAdvisoryLockAsync(_tenantContext.TenantId, $"Bill_{billId}", ct);

        var bill = await _db.Bills
            .FirstOrDefaultAsync(b => b.BillId == billId, ct)
            ?? throw new NotFoundException(nameof(Bill), billId);

        if (bill.Status != BillStatus.Paid && bill.Status != BillStatus.PartiallyPaid)
            throw new AppException($"Cannot void a bill with status '{bill.Status}'.", 400);

        bill.Status = BillStatus.Void;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditActions.BillVoid, nameof(Bill), billId, bill.PatientId, ct: ct);
        _logger.LogWarning("Bill {BillId} voided", billId);

        await transaction.CommitAsync(ct);

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
            .Include(b => b.Adjustments)
                .ThenInclude(a => a.AdjustedBy)
            .Include(b => b.CreditNotes)
                .ThenInclude(cn => cn.CreatedBy)
            .Include(b => b.CreditNotes)
                .ThenInclude(cn => cn.ApprovedBy)
            .Include(b => b.Refunds)
                .ThenInclude(r => r.CreatedBy)
            .Include(b => b.Refunds)
                .ThenInclude(r => r.ProcessedBy)
            .Include(b => b.Refunds)
                .ThenInclude(r => r.CreditNote)
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
        AdjustmentTotal     = b.AdjustmentTotal,
        DiscountAmount      = b.DiscountAmount,
        WriteOffAmount      = b.WriteOffAmount,
        CreditNoteTotal     = b.CreditNoteTotal,
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
        AdjustmentTotal     = b.AdjustmentTotal,
        DiscountAmount      = b.DiscountAmount,
        WriteOffAmount      = b.WriteOffAmount,
        CreditNoteTotal     = b.CreditNoteTotal,
        PaidAmount          = b.PaidAmount,
        BalanceDue          = b.BalanceDue,
        IssuedAt            = b.IssuedAt,
        CreatedAt           = b.CreatedAt,
        ConsultationId      = b.ConsultationId,
        PayerId             = b.PayerId,
        PayerName           = b.Payer?.Name,
        DiscountReason      = b.DiscountReason,
        WriteOffReason      = b.WriteOffReason,
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
        }).ToList(),
        Adjustments = b.Adjustments
            .OrderBy(a => a.AdjustedAt)
            .Select(a => new BillAdjustmentResponse
            {
                BillAdjustmentId = a.BillAdjustmentId,
                Amount           = a.Amount,
                Reason           = a.Reason,
                AdjustedByName   = $"{a.AdjustedBy.FirstName} {a.AdjustedBy.LastName}".Trim(),
                AdjustedAt       = a.AdjustedAt
            }).ToList(),
        CreditNotes = b.CreditNotes
            .OrderByDescending(cn => cn.CreatedAt)
            .Select(cn => new CreditNoteResponse
            {
                CreditNoteId     = cn.CreditNoteId,
                CreditNoteNumber = cn.CreditNoteNumber,
                BillId           = cn.BillId,
                BillNumber       = b.BillNumber,
                PatientId        = cn.PatientId,
                PatientName      = $"{b.Patient.FirstName} {b.Patient.LastName}".Trim(),
                PatientMrn       = b.Patient.MedicalRecordNumber,
                Amount           = cn.Amount,
                Reason           = cn.Reason,
                Status           = cn.Status,
                Notes            = cn.Notes,
                CreatedByName    = $"{cn.CreatedBy.FirstName} {cn.CreatedBy.LastName}".Trim(),
                ApprovedByName   = cn.ApprovedBy == null ? null : $"{cn.ApprovedBy.FirstName} {cn.ApprovedBy.LastName}".Trim(),
                ApprovedAt       = cn.ApprovedAt,
                AppliedAt        = cn.AppliedAt,
                CreatedAt        = cn.CreatedAt,
                UpdatedAt        = cn.UpdatedAt
            }).ToList(),
        Refunds = b.Refunds
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RefundResponse
            {
                RefundId         = r.RefundId,
                RefundNumber     = r.RefundNumber,
                BillId           = r.BillId,
                BillNumber       = b.BillNumber,
                PatientId        = r.PatientId,
                PatientName      = $"{b.Patient.FirstName} {b.Patient.LastName}".Trim(),
                PatientMrn       = b.Patient.MedicalRecordNumber,
                CreditNoteId     = r.CreditNoteId,
                CreditNoteNumber = r.CreditNote?.CreditNoteNumber,
                Amount           = r.Amount,
                Reason           = r.Reason,
                RefundMethod     = r.RefundMethod,
                Reference        = r.Reference,
                Status           = r.Status,
                Notes            = r.Notes,
                CreatedByName    = $"{r.CreatedBy.FirstName} {r.CreatedBy.LastName}".Trim(),
                ProcessedByName  = r.ProcessedBy == null ? null : $"{r.ProcessedBy.FirstName} {r.ProcessedBy.LastName}".Trim(),
                ProcessedAt      = r.ProcessedAt,
                CreatedAt        = r.CreatedAt,
                UpdatedAt        = r.UpdatedAt
            }).ToList()
    };
}
