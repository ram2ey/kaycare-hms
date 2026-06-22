using KayCare.Core.Constants;
using KayCare.Core.DTOs.Billing;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KayCare.Infrastructure.Services;

public class BillingReportsService : IBillingReportsService
{
    private readonly AppDbContext _db;

    public BillingReportsService(AppDbContext db)
    {
        _db = db;
    }

    // ── AR Aging ──────────────────────────────────────────────────────────────

    public async Task<ArAgingReport> GetArAgingAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var bills = await _db.Bills
            .Include(b => b.Patient)
            .Include(b => b.Payer)
            .AsNoTracking()
            .Where(b => b.Status == BillStatus.Issued || b.Status == BillStatus.PartiallyPaid)
            .Where(b => b.BalanceDue > 0)
            .OrderBy(b => b.IssuedAt)
            .ToListAsync(ct);

        var rows = bills.Select(b =>
        {
            var days   = (int)(now - (b.IssuedAt ?? b.CreatedAt)).TotalDays;
            var bucket = days <= 30 ? "0-30"
                       : days <= 60 ? "31-60"
                       : days <= 90 ? "61-90"
                       : "90+";

            return new ArAgingRow
            {
                BillId              = b.BillId,
                BillNumber          = b.BillNumber,
                PatientName         = $"{b.Patient.FirstName} {b.Patient.LastName}".Trim(),
                MedicalRecordNumber = b.Patient.MedicalRecordNumber,
                PayerName           = b.Payer?.Name,
                IssuedAt            = b.IssuedAt ?? b.CreatedAt,
                DaysOutstanding     = days,
                AgingBucket         = bucket,
                TotalAmount         = b.TotalAmount,
                PaidAmount          = b.PaidAmount,
                BalanceDue          = b.BalanceDue,
                Status              = b.Status
            };
        }).ToList();

        return new ArAgingReport
        {
            TotalBalance0To30  = rows.Where(r => r.AgingBucket == "0-30").Sum(r => r.BalanceDue),
            TotalBalance31To60 = rows.Where(r => r.AgingBucket == "31-60").Sum(r => r.BalanceDue),
            TotalBalance61To90 = rows.Where(r => r.AgingBucket == "61-90").Sum(r => r.BalanceDue),
            TotalBalance90Plus = rows.Where(r => r.AgingBucket == "90+").Sum(r => r.BalanceDue),
            GrandTotalBalance  = rows.Sum(r => r.BalanceDue),
            Rows               = rows
        };
    }

    // ── Revenue Dashboard ─────────────────────────────────────────────────────

    public async Task<RevenueDashboardResponse> GetRevenueDashboardAsync(CancellationToken ct = default)
    {
        var now         = DateTime.UtcNow;
        var thirtyAgo   = now.AddDays(-30);
        var startRange  = new DateTime(now.Year, now.Month, 1).AddMonths(-5);

        // ── Headline metrics ──────────────────────────────────────────────────
        var headline = await _db.Bills
            .Where(b => b.Status != BillStatus.Cancelled)
            .GroupBy(b => 1)
            .Select(g => new
            {
                TotalInvoiced    = g.Sum(b => b.TotalAmount),
                TotalCollected   = g.Sum(b => b.PaidAmount),
                TotalOutstanding = g.Where(b => b.Status == BillStatus.Issued || b.Status == BillStatus.PartiallyPaid).Sum(b => b.BalanceDue),
                TotalDiscounts   = g.Sum(b => b.DiscountAmount),
                TotalAdjustments = g.Sum(b => b.AdjustmentTotal),
                TotalWrittenOff  = g.Sum(b => b.WriteOffAmount),
                OutstandingBills = g.Count(b => b.Status == BillStatus.Issued || b.Status == BillStatus.PartiallyPaid),
                OverdueBills     = g.Count(b => (b.Status == BillStatus.Issued || b.Status == BillStatus.PartiallyPaid) && (b.IssuedAt ?? b.CreatedAt) < thirtyAgo),
                TotalBills       = g.Count()
            })
            .FirstOrDefaultAsync(ct);

        var totalInvoiced    = headline?.TotalInvoiced ?? 0m;
        var totalCollected   = headline?.TotalCollected ?? 0m;
        var totalOutstanding = headline?.TotalOutstanding ?? 0m;
        var totalDiscounts   = headline?.TotalDiscounts ?? 0m;
        var totalAdjustments = headline?.TotalAdjustments ?? 0m;
        var totalWrittenOff  = headline?.TotalWrittenOff ?? 0m;
        var totalBills       = headline?.TotalBills ?? 0;
        var outstandingBills = headline?.OutstandingBills ?? 0;
        var overdueBills     = headline?.OverdueBills ?? 0;

        // ── Monthly revenue — last 6 calendar months (aggregated in DB) ───────
        var monthlyInvoicedQuery = await _db.Bills
            .Where(b => b.Status != BillStatus.Cancelled && b.CreatedAt >= startRange)
            .GroupBy(b => new { Year = b.CreatedAt.Year, Month = b.CreatedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(b => b.TotalAmount) })
            .ToListAsync(ct);

        var monthlyCollectedQuery = await _db.Payments
            .Where(p => p.PaymentDate >= startRange)
            .GroupBy(p => new { Year = p.PaymentDate.Year, Month = p.PaymentDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(p => p.Amount) })
            .ToListAsync(ct);

        var invoicedMap  = monthlyInvoicedQuery.ToDictionary(x => (x.Year, x.Month), x => x.Total);
        var collectedMap = monthlyCollectedQuery.ToDictionary(x => (x.Year, x.Month), x => x.Total);

        var monthlyRevenue = new List<MonthlyRevenuePoint>();
        for (int i = 5; i >= 0; i--)
        {
            var month = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
            var label = month.ToString("MMM yyyy");

            monthlyRevenue.Add(new MonthlyRevenuePoint
            {
                Month     = label,
                Invoiced  = invoicedMap.GetValueOrDefault((month.Year, month.Month), 0m),
                Collected = collectedMap.GetValueOrDefault((month.Year, month.Month), 0m)
            });
        }

        // ── By payer (aggregated in DB) ───────────────────────────────────────
        var byPayer = await _db.Bills
            .Where(b => b.Status != BillStatus.Cancelled)
            .GroupBy(b => b.Payer!.Name)
            .Select(g => new PayerRevenueRow
            {
                PayerName   = g.Key ?? "Self-Pay",
                BillCount   = g.Count(),
                Invoiced    = g.Sum(b => b.TotalAmount),
                Collected   = g.Sum(b => b.PaidAmount),
                Outstanding = g.Where(b => b.Status == BillStatus.Issued || b.Status == BillStatus.PartiallyPaid)
                               .Sum(b => b.BalanceDue)
            })
            .OrderByDescending(r => r.Invoiced)
            .ToListAsync(ct);

        // ── By status (aggregated in DB) ──────────────────────────────────────
        var byStatus = await _db.Bills
            .Where(b => b.Status != BillStatus.Cancelled)
            .GroupBy(b => b.Status)
            .Select(g => new StatusCount
            {
                Status = g.Key,
                Count  = g.Count(),
                Total  = g.Sum(b => b.TotalAmount)
            })
            .OrderByDescending(s => s.Count)
            .ToListAsync(ct);

        return new RevenueDashboardResponse
        {
            TotalInvoiced    = totalInvoiced,
            TotalCollected   = totalCollected,
            TotalOutstanding = totalOutstanding,
            TotalDiscounts   = totalDiscounts,
            TotalAdjustments = totalAdjustments,
            TotalWrittenOff  = totalWrittenOff,
            TotalBills       = totalBills,
            OutstandingBills = outstandingBills,
            OverdueBills     = overdueBills,
            MonthlyRevenue   = monthlyRevenue,
            ByPayer          = byPayer,
            ByStatus         = byStatus
        };
    }
}
