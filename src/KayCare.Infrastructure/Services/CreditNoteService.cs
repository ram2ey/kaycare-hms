using KayCare.Core.Constants;
using KayCare.Core.DTOs.Billing;
using KayCare.Core.Entities;
using KayCare.Core.Exceptions;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KayCare.Infrastructure.Services;

public class CreditNoteService : ICreditNoteService
{
    private readonly AppDbContext        _db;
    private readonly ICurrentUserService _currentUser;

    public CreditNoteService(AppDbContext db, ICurrentUserService currentUser)
    {
        _db          = db;
        _currentUser = currentUser;
    }

    // ── Create ────────────────────────────────────────────────────────────────

    public async Task<CreditNoteResponse> CreateAsync(CreateCreditNoteRequest req, CancellationToken ct = default)
    {
        var bill = await _db.Bills.FirstOrDefaultAsync(b => b.BillId == req.BillId, ct)
            ?? throw new NotFoundException(nameof(Bill), req.BillId);

        if (bill.Status == BillStatus.Draft)
            throw new AppException("Cannot issue a credit note against a Draft bill.", 400);

        if (bill.Status == BillStatus.Cancelled || bill.Status == BillStatus.Void)
            throw new AppException($"Cannot issue a credit note against a {bill.Status} bill.", 400);

        if (req.Amount <= 0)
            throw new AppException("Credit note amount must be greater than zero.", 400);

        var number = await GenerateNumberAsync(ct);

        var cn = new CreditNote
        {
            CreditNoteNumber = number,
            BillId           = req.BillId,
            PatientId        = bill.PatientId,
            CreatedByUserId  = _currentUser.UserId,
            Amount           = req.Amount,
            Reason           = req.Reason.Trim(),
            Status           = CreditNoteStatus.Draft,
            Notes            = req.Notes?.Trim()
        };

        _db.CreditNotes.Add(cn);
        await _db.SaveChangesAsync(ct);

        return await LoadDetailAsync(cn.CreditNoteId, ct)
            ?? throw new InvalidOperationException("Failed to load created credit note.");
    }

    // ── List ──────────────────────────────────────────────────────────────────

    public async Task<List<CreditNoteResponse>> GetAllAsync(
        string? status, Guid? billId, Guid? patientId, CancellationToken ct = default)
    {
        var query = _db.CreditNotes
            .Include(c => c.Bill)
            .Include(c => c.Patient)
            .Include(c => c.CreatedBy)
            .Include(c => c.ApprovedBy)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(c => c.Status == status);
        if (billId.HasValue)
            query = query.Where(c => c.BillId == billId.Value);
        if (patientId.HasValue)
            query = query.Where(c => c.PatientId == patientId.Value);

        var list = await query.OrderByDescending(c => c.CreatedAt).ToListAsync(ct);
        return list.Select(MapToResponse).ToList();
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    public async Task<CreditNoteResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await LoadDetailAsync(id, ct);

    // ── Approve ───────────────────────────────────────────────────────────────

    public async Task<CreditNoteResponse> ApproveAsync(Guid id, CancellationToken ct = default)
    {
        var cn = await _db.CreditNotes.FirstOrDefaultAsync(c => c.CreditNoteId == id, ct)
            ?? throw new NotFoundException(nameof(CreditNote), id);

        if (cn.Status != CreditNoteStatus.Draft)
            throw new AppException($"Only Draft credit notes can be approved. Current status: {cn.Status}.", 409);

        cn.Status          = CreditNoteStatus.Approved;
        cn.ApprovedByUserId = _currentUser.UserId;
        cn.ApprovedAt      = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return await LoadDetailAsync(id, ct)
            ?? throw new InvalidOperationException("Failed to load credit note after approval.");
    }

    // ── Apply ─────────────────────────────────────────────────────────────────

    public async Task<CreditNoteResponse> ApplyAsync(Guid id, CancellationToken ct = default)
    {
        var cn = await _db.CreditNotes
            .Include(c => c.Bill)
            .FirstOrDefaultAsync(c => c.CreditNoteId == id, ct)
            ?? throw new NotFoundException(nameof(CreditNote), id);

        if (cn.Status != CreditNoteStatus.Approved)
            throw new AppException($"Only Approved credit notes can be applied. Current status: {cn.Status}.", 409);

        var bill = cn.Bill;

        cn.Status    = CreditNoteStatus.Applied;
        cn.AppliedAt = DateTime.UtcNow;

        // Apply credit to bill
        bill.CreditNoteTotal += cn.Amount;

        // Recalculate bill status — BalanceDue is computed, so compute it manually here
        var effectiveBalance = bill.TotalAmount + bill.AdjustmentTotal
            - bill.DiscountAmount - bill.WriteOffAmount - bill.CreditNoteTotal - bill.PaidAmount;

        if (effectiveBalance <= 0 && bill.Status != BillStatus.Cancelled && bill.Status != BillStatus.Void)
            bill.Status = BillStatus.Paid;
        else if (bill.PaidAmount > 0 && effectiveBalance > 0)
            bill.Status = BillStatus.PartiallyPaid;

        await _db.SaveChangesAsync(ct);

        return await LoadDetailAsync(id, ct)
            ?? throw new InvalidOperationException("Failed to load credit note after applying.");
    }

    // ── Void ──────────────────────────────────────────────────────────────────

    public async Task<CreditNoteResponse> VoidAsync(Guid id, CancellationToken ct = default)
    {
        var cn = await _db.CreditNotes.FirstOrDefaultAsync(c => c.CreditNoteId == id, ct)
            ?? throw new NotFoundException(nameof(CreditNote), id);

        if (cn.Status == CreditNoteStatus.Applied)
            throw new AppException("Applied credit notes cannot be voided. Raise a new bill adjustment instead.", 409);

        if (cn.Status == CreditNoteStatus.Voided)
            throw new AppException("Credit note is already voided.", 409);

        cn.Status = CreditNoteStatus.Voided;

        await _db.SaveChangesAsync(ct);

        return await LoadDetailAsync(id, ct)
            ?? throw new InvalidOperationException("Failed to load credit note after voiding.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<CreditNoteResponse?> LoadDetailAsync(Guid id, CancellationToken ct)
    {
        var cn = await _db.CreditNotes
            .Include(c => c.Bill)
            .Include(c => c.Patient)
            .Include(c => c.CreatedBy)
            .Include(c => c.ApprovedBy)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CreditNoteId == id, ct);

        return cn == null ? null : MapToResponse(cn);
    }

    private static CreditNoteResponse MapToResponse(CreditNote c) => new()
    {
        CreditNoteId     = c.CreditNoteId,
        CreditNoteNumber = c.CreditNoteNumber,
        BillId           = c.BillId,
        BillNumber       = c.Bill.BillNumber,
        PatientId        = c.PatientId,
        PatientName      = $"{c.Patient.FirstName} {c.Patient.LastName}",
        PatientMrn       = c.Patient.MedicalRecordNumber,
        Amount           = c.Amount,
        Reason           = c.Reason,
        Status           = c.Status,
        Notes            = c.Notes,
        CreatedByName    = $"{c.CreatedBy.FirstName} {c.CreatedBy.LastName}",
        ApprovedByName   = c.ApprovedBy == null ? null : $"{c.ApprovedBy.FirstName} {c.ApprovedBy.LastName}",
        ApprovedAt       = c.ApprovedAt,
        AppliedAt        = c.AppliedAt,
        CreatedAt        = c.CreatedAt,
        UpdatedAt        = c.UpdatedAt
    };

    private async Task<string> GenerateNumberAsync(CancellationToken ct)
    {
        var year  = DateTime.UtcNow.Year;
        var count = await _db.CreditNotes.CountAsync(c => c.CreatedAt.Year == year, ct);
        return $"CN-{year}-{(count + 1):D5}";
    }
}
