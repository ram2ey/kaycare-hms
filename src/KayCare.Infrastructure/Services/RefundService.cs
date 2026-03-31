using KayCare.Core.Constants;
using KayCare.Core.DTOs.Billing;
using KayCare.Core.Entities;
using KayCare.Core.Exceptions;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KayCare.Infrastructure.Services;

public class RefundService : IRefundService
{
    private readonly AppDbContext        _db;
    private readonly ICurrentUserService _currentUser;

    public RefundService(AppDbContext db, ICurrentUserService currentUser)
    {
        _db          = db;
        _currentUser = currentUser;
    }

    // ── Create ────────────────────────────────────────────────────────────────

    public async Task<RefundResponse> CreateAsync(CreateRefundRequest req, CancellationToken ct = default)
    {
        var bill = await _db.Bills.FirstOrDefaultAsync(b => b.BillId == req.BillId, ct)
            ?? throw new NotFoundException(nameof(Bill), req.BillId);

        if (!RefundMethod.All.Contains(req.RefundMethod))
            throw new AppException($"Invalid refund method '{req.RefundMethod}'.", 400);

        if (req.Amount <= 0)
            throw new AppException("Refund amount must be greater than zero.", 400);

        if (req.Amount > bill.PaidAmount)
            throw new AppException($"Refund amount ({req.Amount:F2}) cannot exceed the amount already paid ({bill.PaidAmount:F2}).", 400);

        // If linked to a credit note, validate it
        if (req.CreditNoteId.HasValue)
        {
            var cn = await _db.CreditNotes.FirstOrDefaultAsync(c => c.CreditNoteId == req.CreditNoteId.Value, ct)
                ?? throw new NotFoundException(nameof(CreditNote), req.CreditNoteId.Value);

            if (cn.BillId != req.BillId)
                throw new AppException("The credit note does not belong to this bill.", 400);

            if (cn.Status != CreditNoteStatus.Applied)
                throw new AppException("Only Applied credit notes can be linked to a refund.", 400);
        }

        var number = await GenerateNumberAsync(ct);

        var refund = new Refund
        {
            RefundNumber      = number,
            BillId            = req.BillId,
            PatientId         = bill.PatientId,
            CreditNoteId      = req.CreditNoteId,
            CreatedByUserId   = _currentUser.UserId,
            Amount            = req.Amount,
            Reason            = req.Reason.Trim(),
            RefundMethod      = req.RefundMethod.Trim(),
            Reference         = req.Reference?.Trim(),
            Status            = RefundStatus.Pending,
            Notes             = req.Notes?.Trim()
        };

        _db.Refunds.Add(refund);
        await _db.SaveChangesAsync(ct);

        return await LoadDetailAsync(refund.RefundId, ct)
            ?? throw new InvalidOperationException("Failed to load created refund.");
    }

    // ── List ──────────────────────────────────────────────────────────────────

    public async Task<List<RefundResponse>> GetAllAsync(
        string? status, Guid? billId, Guid? patientId, CancellationToken ct = default)
    {
        var query = _db.Refunds
            .Include(r => r.Bill)
            .Include(r => r.Patient)
            .Include(r => r.CreditNote)
            .Include(r => r.CreatedBy)
            .Include(r => r.ProcessedBy)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(r => r.Status == status);
        if (billId.HasValue)
            query = query.Where(r => r.BillId == billId.Value);
        if (patientId.HasValue)
            query = query.Where(r => r.PatientId == patientId.Value);

        var list = await query.OrderByDescending(r => r.CreatedAt).ToListAsync(ct);
        return list.Select(MapToResponse).ToList();
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    public async Task<RefundResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await LoadDetailAsync(id, ct);

    // ── Process ───────────────────────────────────────────────────────────────

    public async Task<RefundResponse> ProcessAsync(Guid id, CancellationToken ct = default)
    {
        var refund = await _db.Refunds
            .Include(r => r.Bill)
            .FirstOrDefaultAsync(r => r.RefundId == id, ct)
            ?? throw new NotFoundException(nameof(Refund), id);

        if (refund.Status != RefundStatus.Pending)
            throw new AppException($"Only Pending refunds can be processed. Current status: {refund.Status}.", 409);

        var bill = refund.Bill;

        if (refund.Amount > bill.PaidAmount)
            throw new AppException($"Refund amount ({refund.Amount:F2}) exceeds current paid amount ({bill.PaidAmount:F2}).", 400);

        // Record the refund payout — reduce PaidAmount on the bill
        bill.PaidAmount -= refund.Amount;

        // Recalculate bill status
        var effectiveBalance = bill.TotalAmount + bill.AdjustmentTotal
            - bill.DiscountAmount - bill.WriteOffAmount - bill.CreditNoteTotal - bill.PaidAmount;

        if (effectiveBalance <= 0)
            bill.Status = BillStatus.Paid;
        else if (bill.PaidAmount > 0)
            bill.Status = BillStatus.PartiallyPaid;
        else if (bill.Status == BillStatus.Paid || bill.Status == BillStatus.PartiallyPaid)
            bill.Status = BillStatus.Issued;

        refund.Status          = RefundStatus.Processed;
        refund.ProcessedByUserId = _currentUser.UserId;
        refund.ProcessedAt     = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return await LoadDetailAsync(id, ct)
            ?? throw new InvalidOperationException("Failed to load refund after processing.");
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    public async Task<RefundResponse> CancelAsync(Guid id, CancellationToken ct = default)
    {
        var refund = await _db.Refunds.FirstOrDefaultAsync(r => r.RefundId == id, ct)
            ?? throw new NotFoundException(nameof(Refund), id);

        if (refund.Status != RefundStatus.Pending)
            throw new AppException($"Only Pending refunds can be cancelled. Current status: {refund.Status}.", 409);

        refund.Status = RefundStatus.Cancelled;

        await _db.SaveChangesAsync(ct);

        return await LoadDetailAsync(id, ct)
            ?? throw new InvalidOperationException("Failed to load refund after cancellation.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<RefundResponse?> LoadDetailAsync(Guid id, CancellationToken ct)
    {
        var refund = await _db.Refunds
            .Include(r => r.Bill)
            .Include(r => r.Patient)
            .Include(r => r.CreditNote)
            .Include(r => r.CreatedBy)
            .Include(r => r.ProcessedBy)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RefundId == id, ct);

        return refund == null ? null : MapToResponse(refund);
    }

    private static RefundResponse MapToResponse(Refund r) => new()
    {
        RefundId         = r.RefundId,
        RefundNumber     = r.RefundNumber,
        BillId           = r.BillId,
        BillNumber       = r.Bill.BillNumber,
        PatientId        = r.PatientId,
        PatientName      = $"{r.Patient.FirstName} {r.Patient.LastName}",
        PatientMrn       = r.Patient.MedicalRecordNumber,
        CreditNoteId     = r.CreditNoteId,
        CreditNoteNumber = r.CreditNote?.CreditNoteNumber,
        Amount           = r.Amount,
        Reason           = r.Reason,
        RefundMethod     = r.RefundMethod,
        Reference        = r.Reference,
        Status           = r.Status,
        Notes            = r.Notes,
        CreatedByName    = $"{r.CreatedBy.FirstName} {r.CreatedBy.LastName}",
        ProcessedByName  = r.ProcessedBy == null ? null : $"{r.ProcessedBy.FirstName} {r.ProcessedBy.LastName}",
        ProcessedAt      = r.ProcessedAt,
        CreatedAt        = r.CreatedAt,
        UpdatedAt        = r.UpdatedAt
    };

    private async Task<string> GenerateNumberAsync(CancellationToken ct)
    {
        var year  = DateTime.UtcNow.Year;
        var count = await _db.Refunds.CountAsync(r => r.CreatedAt.Year == year, ct);
        return $"REF-{year}-{(count + 1):D5}";
    }
}
