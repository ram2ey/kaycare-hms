using KayCare.Core.Constants;
using KayCare.Core.DTOs.Inpatient;
using KayCare.Core.Entities;
using KayCare.Core.Exceptions;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KayCare.Infrastructure.Services;

public class InpatientBillingService : IInpatientBillingService
{
    private readonly AppDbContext        _db;
    private readonly ITenantContext      _tenantContext;
    private readonly ICurrentUserService _currentUser;

    public InpatientBillingService(AppDbContext db, ITenantContext tenantContext, ICurrentUserService currentUser)
    {
        _db            = db;
        _tenantContext = tenantContext;
        _currentUser   = currentUser;
    }

    public async Task<List<InpatientChargeResponse>> GetChargesAsync(Guid admissionId, CancellationToken ct = default)
    {
        _ = await _db.Admissions.FirstOrDefaultAsync(a => a.AdmissionId == admissionId, ct)
            ?? throw new NotFoundException("Admission", admissionId);

        return await _db.InpatientCharges
            .AsNoTracking()
            .Include(c => c.CreatedBy)
            .Where(c => c.AdmissionId == admissionId)
            .OrderBy(c => c.ChargeDate)
            .ThenBy(c => c.CreatedAt)
            .Select(c => ToResponse(c))
            .ToListAsync(ct);
    }

    public async Task<InpatientBillSummaryResponse> GetBillSummaryAsync(Guid admissionId, CancellationToken ct = default)
    {
        var admission = await _db.Admissions
            .AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Ward)
            .Include(a => a.Transfers.OrderBy(t => t.TransferredAt))
                .ThenInclude(t => t.FromWard)
            .Include(a => a.Transfers.OrderBy(t => t.TransferredAt))
                .ThenInclude(t => t.ToWard)
            .FirstOrDefaultAsync(a => a.AdmissionId == admissionId, ct)
            ?? throw new NotFoundException("Admission", admissionId);

        var charges = await _db.InpatientCharges
            .AsNoTracking()
            .Include(c => c.CreatedBy)
            .Where(c => c.AdmissionId == admissionId)
            .OrderBy(c => c.ChargeDate)
            .ThenBy(c => c.CreatedAt)
            .ToListAsync(ct);

        var bill = await _db.Bills
            .AsNoTracking()
            .Where(b => b.AdmissionId == admissionId && b.Status != "Cancelled")
            .OrderByDescending(b => b.CreatedAt)
            .FirstOrDefaultAsync(ct);

        return new InpatientBillSummaryResponse
        {
            AdmissionId            = admission.AdmissionId,
            AdmissionNumber        = admission.AdmissionNumber,
            PatientName            = $"{admission.Patient.FirstName} {admission.Patient.LastName}",
            PatientMrn             = admission.Patient.MedicalRecordNumber,
            TotalCharges           = charges.Sum(c => c.TotalPrice),
            BillId                 = bill?.BillId,
            BillNumber             = bill?.BillNumber,
            BillStatus             = bill?.Status,
            AccommodationBreakdown = BuildAccommodationBreakdown(admission),
            Charges                = charges.Select(ToResponse).ToList(),
        };
    }

    public async Task<InpatientChargeResponse> AddChargeAsync(
        Guid admissionId, SaveInpatientChargeRequest request, CancellationToken ct = default)
    {
        _ = await _db.Admissions.FirstOrDefaultAsync(a => a.AdmissionId == admissionId, ct)
            ?? throw new NotFoundException("Admission", admissionId);

        if (!InpatientChargeCategory.All.Contains(request.Category))
            throw new AppException($"Invalid category '{request.Category}'.", 400);

        var charge = new InpatientCharge
        {
            TenantId          = _tenantContext.TenantId,
            AdmissionId       = admissionId,
            ChargeDate        = request.ChargeDate,
            Category          = request.Category,
            Description       = request.Description.Trim(),
            Quantity          = request.Quantity,
            UnitPrice         = request.UnitPrice,
            TotalPrice        = request.Quantity * request.UnitPrice,
            Notes             = request.Notes?.Trim(),
            CreatedByUserId   = _currentUser.UserId,
            CreatedAt         = DateTime.UtcNow,
        };

        _db.InpatientCharges.Add(charge);
        await _db.SaveChangesAsync(ct);

        await _db.Entry(charge).Reference(c => c.CreatedBy).LoadAsync(ct);
        return ToResponse(charge);
    }

    public async Task<InpatientChargeResponse> UpdateChargeAsync(
        Guid chargeId, SaveInpatientChargeRequest request, CancellationToken ct = default)
    {
        var charge = await _db.InpatientCharges
            .Include(c => c.CreatedBy)
            .FirstOrDefaultAsync(c => c.InpatientChargeId == chargeId, ct)
            ?? throw new NotFoundException("InpatientCharge", chargeId);

        if (!InpatientChargeCategory.All.Contains(request.Category))
            throw new AppException($"Invalid category '{request.Category}'.", 400);

        charge.ChargeDate  = request.ChargeDate;
        charge.Category    = request.Category;
        charge.Description = request.Description.Trim();
        charge.Quantity    = request.Quantity;
        charge.UnitPrice   = request.UnitPrice;
        charge.TotalPrice  = request.Quantity * request.UnitPrice;
        charge.Notes       = request.Notes?.Trim();

        await _db.SaveChangesAsync(ct);
        return ToResponse(charge);
    }

    public async Task RemoveChargeAsync(Guid chargeId, CancellationToken ct = default)
    {
        var charge = await _db.InpatientCharges
            .FirstOrDefaultAsync(c => c.InpatientChargeId == chargeId, ct)
            ?? throw new NotFoundException("InpatientCharge", chargeId);

        _db.InpatientCharges.Remove(charge);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<InpatientChargeResponse>> ApplyAccommodationChargesAsync(
        Guid admissionId, CancellationToken ct = default)
    {
        var admission = await _db.Admissions
            .Include(a => a.Ward)
            .Include(a => a.Transfers.OrderBy(t => t.TransferredAt))
                .ThenInclude(t => t.FromWard)
            .Include(a => a.Transfers.OrderBy(t => t.TransferredAt))
                .ThenInclude(t => t.ToWard)
            .FirstOrDefaultAsync(a => a.AdmissionId == admissionId, ct)
            ?? throw new NotFoundException("Admission", admissionId);

        // Remove existing accommodation charges for this admission
        var existing = await _db.InpatientCharges
            .Where(c => c.AdmissionId == admissionId && c.Category == InpatientChargeCategory.Accommodation)
            .ToListAsync(ct);
        _db.InpatientCharges.RemoveRange(existing);

        var segments = BuildAccommodationBreakdown(admission);
        var now      = DateTime.UtcNow;
        var charges  = new List<InpatientCharge>();

        foreach (var seg in segments)
        {
            var charge = new InpatientCharge
            {
                TenantId        = _tenantContext.TenantId,
                AdmissionId     = admissionId,
                ChargeDate      = DateOnly.FromDateTime(seg.From),
                Category        = InpatientChargeCategory.Accommodation,
                Description     = $"Accommodation — {seg.WardName} ({seg.Days} day{(seg.Days != 1 ? "s" : "")})",
                Quantity        = seg.Days,
                UnitPrice       = seg.DailyRate,
                TotalPrice      = seg.Total,
                CreatedByUserId = _currentUser.UserId,
                CreatedAt       = now,
            };
            charges.Add(charge);
        }

        _db.InpatientCharges.AddRange(charges);
        await _db.SaveChangesAsync(ct);

        foreach (var c in charges)
            await _db.Entry(c).Reference(x => x.CreatedBy).LoadAsync(ct);

        return charges.Select(ToResponse).ToList();
    }

    public async Task<Guid> GenerateBillAsync(Guid admissionId, CancellationToken ct = default)
    {
        var admission = await _db.Admissions
            .Include(a => a.Patient)
            .FirstOrDefaultAsync(a => a.AdmissionId == admissionId, ct)
            ?? throw new NotFoundException("Admission", admissionId);

        var charges = await _db.InpatientCharges
            .Where(c => c.AdmissionId == admissionId)
            .ToListAsync(ct);

        // Find or create bill linked to this admission
        var bill = await _db.Bills
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.AdmissionId == admissionId && b.Status != "Cancelled", ct);

        if (bill == null)
        {
            var year  = DateTime.UtcNow.Year;
            var count = await _db.Bills.CountAsync(b => b.CreatedAt.Year == year, ct);
            var billNum = $"INV-{year}-{(count + 1):D5}";

            bill = new Bill
            {
                BillNumber      = billNum,
                PatientId       = admission.PatientId,
                AdmissionId     = admissionId,
                CreatedByUserId = _currentUser.UserId,
                Status          = "Draft",
                TotalAmount     = 0,
                PaidAmount      = 0,
            };
            _db.Bills.Add(bill);
            await _db.SaveChangesAsync(ct);

            await _db.Entry(bill).Collection(b => b.Items).LoadAsync(ct);
        }

        // Remove and replace all inpatient charge items
        var oldItems = bill.Items
            .Where(i => i.SourceType == ChargeSourceType.InpatientCharge)
            .ToList();
        _db.BillItems.RemoveRange(oldItems);

        foreach (var c in charges)
        {
            _db.BillItems.Add(new BillItem
            {
                TenantId    = _tenantContext.TenantId,
                BillId      = bill.BillId,
                Description = c.Description,
                Category    = c.Category,
                Quantity    = c.Quantity,
                UnitPrice   = c.UnitPrice,
                TotalPrice  = c.TotalPrice,
                SourceType  = ChargeSourceType.InpatientCharge,
                SourceId    = c.InpatientChargeId,
            });
        }

        // Recalculate total; BalanceDue is a stored computed column — DB handles it
        bill.TotalAmount = charges.Sum(c => c.TotalPrice);

        await _db.SaveChangesAsync(ct);
        return bill.BillId;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    private static List<AccommodationSegment> BuildAccommodationBreakdown(Admission admission)
    {
        var endDate    = admission.ActualDischargeDate ?? DateTime.UtcNow;
        var transfers  = admission.Transfers.OrderBy(t => t.TransferredAt).ToList();
        var segments   = new List<AccommodationSegment>();

        if (transfers.Count == 0)
        {
            var days = Math.Max(1, (int)Math.Ceiling((endDate - admission.AdmissionDate).TotalDays));
            segments.Add(new AccommodationSegment
            {
                WardName  = admission.Ward.Name,
                WardType  = admission.Ward.WardType,
                From      = admission.AdmissionDate,
                To        = endDate,
                Days      = days,
                DailyRate = admission.Ward.DailyRate,
                Total     = days * admission.Ward.DailyRate,
            });
        }
        else
        {
            // First segment: admission date → first transfer, ward = FromWard of first transfer
            var firstTransfer = transfers[0];
            var days0 = Math.Max(1, (int)Math.Ceiling((firstTransfer.TransferredAt - admission.AdmissionDate).TotalDays));
            segments.Add(new AccommodationSegment
            {
                WardName  = firstTransfer.FromWard.Name,
                WardType  = firstTransfer.FromWard.WardType,
                From      = admission.AdmissionDate,
                To        = firstTransfer.TransferredAt,
                Days      = days0,
                DailyRate = firstTransfer.FromWard.DailyRate,
                Total     = days0 * firstTransfer.FromWard.DailyRate,
            });

            // Middle segments
            for (var i = 0; i < transfers.Count - 1; i++)
            {
                var from = transfers[i];
                var to   = transfers[i + 1];
                var d    = Math.Max(1, (int)Math.Ceiling((to.TransferredAt - from.TransferredAt).TotalDays));
                segments.Add(new AccommodationSegment
                {
                    WardName  = from.ToWard.Name,
                    WardType  = from.ToWard.WardType,
                    From      = from.TransferredAt,
                    To        = to.TransferredAt,
                    Days      = d,
                    DailyRate = from.ToWard.DailyRate,
                    Total     = d * from.ToWard.DailyRate,
                });
            }

            // Last segment: last transfer → end date, ward = ToWard of last transfer
            var lastTransfer = transfers[^1];
            var dLast = Math.Max(1, (int)Math.Ceiling((endDate - lastTransfer.TransferredAt).TotalDays));
            segments.Add(new AccommodationSegment
            {
                WardName  = lastTransfer.ToWard.Name,
                WardType  = lastTransfer.ToWard.WardType,
                From      = lastTransfer.TransferredAt,
                To        = endDate,
                Days      = dLast,
                DailyRate = lastTransfer.ToWard.DailyRate,
                Total     = dLast * lastTransfer.ToWard.DailyRate,
            });
        }

        return segments;
    }

    private static InpatientChargeResponse ToResponse(InpatientCharge c) => new()
    {
        InpatientChargeId = c.InpatientChargeId,
        AdmissionId       = c.AdmissionId,
        ChargeDate        = c.ChargeDate.ToString("yyyy-MM-dd"),
        Category          = c.Category,
        Description       = c.Description,
        Quantity          = c.Quantity,
        UnitPrice         = c.UnitPrice,
        TotalPrice        = c.TotalPrice,
        Notes             = c.Notes,
        CreatedByName     = $"{c.CreatedBy.FirstName} {c.CreatedBy.LastName}",
        CreatedAt         = c.CreatedAt,
    };
}
