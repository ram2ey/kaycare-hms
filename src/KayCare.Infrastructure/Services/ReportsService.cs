using KayCare.Core.DTOs.Reports;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KayCare.Infrastructure.Services;

public class ReportsService : IReportsService
{
    private readonly AppDbContext _db;

    public ReportsService(AppDbContext db) => _db = db;

    // ── Clinical ─────────────────────────────────────────────────────────────

    public async Task<ClinicalReportResponse> GetClinicalAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var fromDt = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toDt   = to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var consultations = await _db.Consultations
            .Where(c => c.CreatedAt >= fromDt && c.CreatedAt <= toDt)
            .AsNoTracking()
            .ToListAsync(ct);

        var appointments = await _db.Appointments
            .Where(a => a.ScheduledAt >= fromDt && a.ScheduledAt <= toDt)
            .AsNoTracking()
            .ToListAsync(ct);

        var newPatients = await _db.Patients
            .Where(p => p.CreatedAt >= fromDt && p.CreatedAt <= toDt)
            .CountAsync(ct);

        var byDay = consultations
            .GroupBy(c => c.CreatedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DailyCount(g.Key.ToString("yyyy-MM-dd"), g.Count()))
            .ToList();

        var topDx = consultations
            .Where(c => !string.IsNullOrWhiteSpace(c.PrimaryDiagnosisDesc))
            .GroupBy(c => c.PrimaryDiagnosisDesc!)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => new NameCount(g.Key, g.Count()))
            .ToList();

        var byStatus = appointments
            .GroupBy(a => a.Status)
            .Select(g => new NameCount(g.Key, g.Count()))
            .ToList();

        return new ClinicalReportResponse(
            consultations.Count,
            consultations.Count(c => c.SignedAt != null),
            appointments.Count,
            appointments.Count(a => a.Status == "Completed"),
            newPatients,
            byDay,
            topDx,
            byStatus
        );
    }

    // ── Inpatient ────────────────────────────────────────────────────────────

    public async Task<InpatientReportResponse> GetInpatientAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var fromDt = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toDt   = to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var admissions = await _db.Admissions
            .Include(a => a.Ward)
            .Where(a => a.CreatedAt >= fromDt && a.CreatedAt <= toDt)
            .AsNoTracking()
            .ToListAsync(ct);

        var allBeds     = await _db.Beds.CountAsync(ct);
        var occupiedBeds = await _db.Beds.CountAsync(b => b.Status == "Occupied", ct);

        var discharges = admissions.Where(a => a.Status == "Discharged" && a.ActualDischargeDate != null).ToList();
        var avgLos = discharges.Count == 0 ? 0m :
            discharges.Average(a => (decimal)(a.ActualDischargeDate!.Value - a.AdmissionDate).TotalDays);

        var byDay = admissions
            .GroupBy(a => a.CreatedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DailyCount(g.Key.ToString("yyyy-MM-dd"), g.Count()))
            .ToList();

        var byWard = admissions
            .GroupBy(a => a.Ward.Name)
            .OrderByDescending(g => g.Count())
            .Select(g => new NameCount(g.Key, g.Count()))
            .ToList();

        var byDischargeType = discharges
            .Where(a => !string.IsNullOrWhiteSpace(a.DischargeType))
            .GroupBy(a => a.DischargeType!)
            .Select(g => new NameCount(g.Key, g.Count()))
            .ToList();

        var occupancy = allBeds == 0 ? 0m : Math.Round((decimal)occupiedBeds / allBeds * 100, 1);

        return new InpatientReportResponse(
            admissions.Count,
            admissions.Count(a => a.Status == "Active"),
            discharges.Count,
            Math.Round(avgLos, 1),
            allBeds,
            occupiedBeds,
            occupancy,
            byDay,
            byWard,
            byDischargeType
        );
    }

    // ── Lab ──────────────────────────────────────────────────────────────────

    public async Task<LabReportResponse> GetLabAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var fromDt = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toDt   = to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var orders = await _db.LabOrders
            .Include(o => o.Items)
            .Where(o => o.CreatedAt >= fromDt && o.CreatedAt <= toDt)
            .AsNoTracking()
            .ToListAsync(ct);

        var byDay = orders
            .GroupBy(o => o.CreatedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DailyCount(g.Key.ToString("yyyy-MM-dd"), g.Count()))
            .ToList();

        var topTests = orders
            .SelectMany(o => o.Items)
            .GroupBy(i => i.TestName)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => new NameCount(g.Key, g.Count()))
            .ToList();

        var byStatus = orders
            .GroupBy(o => o.Status)
            .Select(g => new NameCount(g.Key, g.Count()))
            .ToList();

        return new LabReportResponse(
            orders.Count,
            orders.Count(o => o.Status is "Completed" or "Signed"),
            orders.Count(o => o.Status is "Pending" or "Active" or "PartiallyCompleted"),
            byDay,
            topTests,
            byStatus
        );
    }

    // ── Referrals ────────────────────────────────────────────────────────────

    public async Task<ReferralReportResponse> GetReferralsAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var fromDt = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toDt   = to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var referrals = await _db.Referrals
            .Include(r => r.ReferringDoctor)
            .Where(r => r.CreatedAt >= fromDt && r.CreatedAt <= toDt)
            .AsNoTracking()
            .ToListAsync(ct);

        var byStatus = referrals
            .GroupBy(r => r.Status)
            .Select(g => new NameCount(g.Key, g.Count()))
            .ToList();

        var byUrgency = referrals
            .GroupBy(r => r.Urgency)
            .Select(g => new NameCount(g.Key, g.Count()))
            .ToList();

        var topDoctors = referrals
            .GroupBy(r => $"{r.ReferringDoctor.FirstName} {r.ReferringDoctor.LastName}")
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => new NameCount(g.Key, g.Count()))
            .ToList();

        return new ReferralReportResponse(
            referrals.Count,
            referrals.Count(r => r.ReferralType == "Internal"),
            referrals.Count(r => r.ReferralType == "External"),
            referrals.Count(r => r.Status == "Accepted"),
            referrals.Count(r => r.Status == "Declined"),
            referrals.Count(r => r.Status is "Draft" or "Sent"),
            byStatus,
            byUrgency,
            topDoctors
        );
    }

    // ── Pharmacy ─────────────────────────────────────────────────────────────

    public async Task<PharmacyReportResponse> GetPharmacyAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var fromDt = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toDt   = to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var movements = await _db.StockMovements
            .Include(m => m.DrugInventory)
            .Where(m => m.CreatedAt >= fromDt && m.CreatedAt <= toDt && m.MovementType == "Dispense")
            .AsNoTracking()
            .ToListAsync(ct);

        var dispenseEvents = await _db.DispenseEvents
            .Where(e => e.DispensedAt >= fromDt && e.DispensedAt <= toDt)
            .CountAsync(ct);

        var drugs = await _db.DrugInventory
            .Where(d => d.IsActive)
            .AsNoTracking()
            .ToListAsync(ct);

        var topDrugs = movements
            .GroupBy(m => m.DrugInventory.Name)
            .OrderByDescending(g => g.Sum(m => m.Quantity))
            .Take(10)
            .Select(g => new NameCount(g.Key, g.Sum(m => m.Quantity)))
            .ToList();

        var byCategory = drugs
            .GroupBy(d => d.Category ?? "Uncategorised")
            .OrderByDescending(g => g.Count())
            .Select(g => new NameCount(g.Key, g.Count()))
            .ToList();

        return new PharmacyReportResponse(
            dispenseEvents,
            drugs.Count(d => d.CurrentStock > 0 && d.CurrentStock <= d.ReorderThreshold),
            drugs.Count(d => d.CurrentStock == 0),
            drugs.Count,
            topDrugs,
            byCategory
        );
    }
}
