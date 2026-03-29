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

        var bills = await _db.Bills
            .Include(b => b.Payer)
            .AsNoTracking()
            .ToListAsync(ct);

        var payments = await _db.Payments
            .AsNoTracking()
            .ToListAsync(ct);

        // ── Headline metrics ──────────────────────────────────────────────────

        var activeBills = bills.Where(b => b.Status != BillStatus.Cancelled).ToList();

        var totalInvoiced    = activeBills.Sum(b => b.TotalAmount);
        var totalCollected   = activeBills.Sum(b => b.PaidAmount);
        var totalOutstanding = activeBills
            .Where(b => b.Status == BillStatus.Issued || b.Status == BillStatus.PartiallyPaid)
            .Sum(b => b.BalanceDue);
        var totalDiscounts   = activeBills.Sum(b => b.DiscountAmount);
        var totalAdjustments = activeBills.Sum(b => b.AdjustmentTotal);
        var totalWrittenOff  = activeBills.Sum(b => b.WriteOffAmount);
        var outstandingBills = activeBills
            .Count(b => b.Status == BillStatus.Issued || b.Status == BillStatus.PartiallyPaid);
        var overdueBills = activeBills
            .Count(b => (b.Status == BillStatus.Issued || b.Status == BillStatus.PartiallyPaid)
                     && (b.IssuedAt ?? b.CreatedAt) < thirtyAgo);

        // ── Monthly revenue — last 6 calendar months ──────────────────────────

        var monthlyRevenue = new List<MonthlyRevenuePoint>();
        for (int i = 5; i >= 0; i--)
        {
            var month     = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
            var monthEnd  = month.AddMonths(1);
            var label     = month.ToString("MMM yyyy");

            var invoiced  = activeBills
                .Where(b => b.CreatedAt >= month && b.CreatedAt < monthEnd)
                .Sum(b => b.TotalAmount);

            var collected = payments
                .Where(p => p.PaymentDate >= month && p.PaymentDate < monthEnd)
                .Sum(p => p.Amount);

            monthlyRevenue.Add(new MonthlyRevenuePoint
            {
                Month     = label,
                Invoiced  = invoiced,
                Collected = collected
            });
        }

        // ── By payer ──────────────────────────────────────────────────────────

        var byPayer = activeBills
            .GroupBy(b => b.Payer?.Name ?? "Self-Pay")
            .Select(g => new PayerRevenueRow
            {
                PayerName   = g.Key,
                BillCount   = g.Count(),
                Invoiced    = g.Sum(b => b.TotalAmount),
                Collected   = g.Sum(b => b.PaidAmount),
                Outstanding = g.Where(b => b.Status == BillStatus.Issued || b.Status == BillStatus.PartiallyPaid)
                               .Sum(b => b.BalanceDue)
            })
            .OrderByDescending(r => r.Invoiced)
            .ToList();

        // ── By status ─────────────────────────────────────────────────────────

        var byStatus = activeBills
            .GroupBy(b => b.Status)
            .Select(g => new StatusCount
            {
                Status = g.Key,
                Count  = g.Count(),
                Total  = g.Sum(b => b.TotalAmount)
            })
            .OrderByDescending(s => s.Count)
            .ToList();

        return new RevenueDashboardResponse
        {
            TotalInvoiced    = totalInvoiced,
            TotalCollected   = totalCollected,
            TotalOutstanding = totalOutstanding,
            TotalDiscounts   = totalDiscounts,
            TotalAdjustments = totalAdjustments,
            TotalWrittenOff  = totalWrittenOff,
            TotalBills       = activeBills.Count,
            OutstandingBills = outstandingBills,
            OverdueBills     = overdueBills,
            MonthlyRevenue   = monthlyRevenue,
            ByPayer          = byPayer,
            ByStatus         = byStatus
        };
    }
}
