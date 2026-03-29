using KayCare.Core.Constants;
using KayCare.Core.Entities;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KayCare.Infrastructure.Services;

public class ChargeCaptureService : IChargeCaptureService
{
    private readonly AppDbContext        _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext      _tenantContext;

    public ChargeCaptureService(AppDbContext db, ICurrentUserService currentUser, ITenantContext tenantContext)
    {
        _db           = db;
        _currentUser  = currentUser;
        _tenantContext = tenantContext;
    }

    // ── Consultation ──────────────────────────────────────────────────────────

    public async Task CaptureConsultationChargeAsync(Guid consultationId, CancellationToken ct = default)
    {
        var consultation = await _db.Consultations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ConsultationId == consultationId, ct);
        if (consultation is null) return;

        // Idempotency: skip if already captured
        var alreadyCaptured = await _db.BillItems
            .AnyAsync(i => i.SourceType == ChargeSourceType.Consultation
                        && i.SourceId   == consultationId, ct);
        if (alreadyCaptured) return;

        var bill  = await FindOrCreateBillAsync(consultation.PatientId, consultationId, ct);
        var price = await GetCatalogPriceAsync("Medical Consultation", "Consultation", ct);

        _db.BillItems.Add(new BillItem
        {
            BillId      = bill.BillId,
            TenantId    = _tenantContext.TenantId,
            Description = "Medical Consultation",
            Category    = "Consultation",
            Quantity    = 1,
            UnitPrice   = price,
            SourceType  = ChargeSourceType.Consultation,
            SourceId    = consultationId
        });

        await _db.SaveChangesAsync(ct);
        await RecalculateTotalAsync(bill.BillId, ct);
    }

    // ── Lab Order ─────────────────────────────────────────────────────────────

    public async Task CaptureLabOrderChargesAsync(Guid labOrderId, CancellationToken ct = default)
    {
        var order = await _db.LabOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.LabOrderId == labOrderId, ct);
        if (order is null) return;

        var bill = await FindOrCreateBillAsync(order.PatientId, order.ConsultationId, ct);

        // Link the order to the bill
        if (order.BillId != bill.BillId)
        {
            order.BillId = bill.BillId;
            await _db.SaveChangesAsync(ct);
        }

        // Batch-load catalog once
        var catalog = await _db.ServiceCatalogItems
            .Where(s => s.IsActive)
            .AsNoTracking()
            .ToListAsync(ct);

        var anyAdded = false;
        foreach (var labItem in order.Items)
        {
            var alreadyCaptured = await _db.BillItems
                .AnyAsync(i => i.SourceType == ChargeSourceType.LabOrder
                            && i.SourceId   == labItem.LabOrderItemId, ct);
            if (alreadyCaptured) continue;

            var price = FindCatalogPrice(catalog, labItem.TestName, "Laboratory");

            _db.BillItems.Add(new BillItem
            {
                BillId      = bill.BillId,
                TenantId    = _tenantContext.TenantId,
                Description = labItem.TestName,
                Category    = "Laboratory",
                Quantity    = 1,
                UnitPrice   = price,
                SourceType  = ChargeSourceType.LabOrder,
                SourceId    = labItem.LabOrderItemId
            });
            anyAdded = true;
        }

        if (anyAdded)
        {
            await _db.SaveChangesAsync(ct);
            await RecalculateTotalAsync(bill.BillId, ct);
        }
    }

    // ── Prescription Dispense ─────────────────────────────────────────────────

    public async Task CaptureDispenseChargesAsync(Guid prescriptionId, Guid dispenseEventId, CancellationToken ct = default)
    {
        var prescription = await _db.Prescriptions
            .FirstOrDefaultAsync(p => p.PrescriptionId == prescriptionId, ct);
        if (prescription is null) return;

        var dispenseEvent = await _db.DispenseEvents
            .Include(e => e.Items)
                .ThenInclude(i => i.PrescriptionItem)
            .FirstOrDefaultAsync(e => e.DispenseEventId == dispenseEventId, ct);
        if (dispenseEvent is null) return;

        var bill = await FindOrCreateBillAsync(prescription.PatientId, prescription.ConsultationId, ct);

        // Link prescription to bill
        if (prescription.BillId != bill.BillId)
        {
            prescription.BillId = bill.BillId;
            await _db.SaveChangesAsync(ct);
        }

        var catalog = await _db.ServiceCatalogItems
            .Where(s => s.IsActive)
            .AsNoTracking()
            .ToListAsync(ct);

        foreach (var eventItem in dispenseEvent.Items)
        {
            var med      = eventItem.PrescriptionItem;
            var price    = FindCatalogPrice(catalog, med.MedicationName, "Medication");
            var desc     = string.IsNullOrWhiteSpace(med.Strength)
                ? med.MedicationName
                : $"{med.MedicationName} {med.Strength}";

            _db.BillItems.Add(new BillItem
            {
                BillId      = bill.BillId,
                TenantId    = _tenantContext.TenantId,
                Description = desc,
                Category    = "Medication",
                Quantity    = eventItem.QuantityDispensed,
                UnitPrice   = price,
                SourceType  = ChargeSourceType.Prescription,
                SourceId    = eventItem.DispenseEventItemId
            });
        }

        await _db.SaveChangesAsync(ct);
        await RecalculateTotalAsync(bill.BillId, ct);
    }

    // ── Shared helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Returns an existing Draft or Issued bill for the consultation, or creates a new Draft bill.
    /// </summary>
    private async Task<Bill> FindOrCreateBillAsync(Guid patientId, Guid? consultationId, CancellationToken ct)
    {
        if (consultationId.HasValue)
        {
            var existing = await _db.Bills
                .FirstOrDefaultAsync(b => b.ConsultationId == consultationId
                                       && (b.Status == BillStatus.Draft || b.Status == BillStatus.Issued), ct);
            if (existing is not null) return existing;
        }

        var billNumber = await GenerateBillNumberAsync(ct);
        var bill = new Bill
        {
            BillNumber      = billNumber,
            PatientId       = patientId,
            ConsultationId  = consultationId,
            CreatedByUserId = _currentUser.UserId,
            Status          = BillStatus.Draft,
            TotalAmount     = 0m,
            PaidAmount      = 0m
        };

        _db.Bills.Add(bill);
        await _db.SaveChangesAsync(ct);
        return bill;
    }

    /// <summary>Recalculates Bill.TotalAmount from its current BillItems.</summary>
    private async Task RecalculateTotalAsync(Guid billId, CancellationToken ct)
    {
        var bill = await _db.Bills.FirstOrDefaultAsync(b => b.BillId == billId, ct);
        if (bill is null) return;

        bill.TotalAmount = await _db.BillItems
            .Where(i => i.BillId == billId)
            .SumAsync(i => i.TotalPrice, ct);

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Looks up a price by item name first, then category fallback. Returns 0 if not found.</summary>
    private async Task<decimal> GetCatalogPriceAsync(string name, string category, CancellationToken ct)
    {
        var byName = await _db.ServiceCatalogItems
            .Where(s => s.IsActive && s.Name.ToLower() == name.ToLower())
            .Select(s => (decimal?)s.UnitPrice)
            .FirstOrDefaultAsync(ct);

        if (byName.HasValue) return byName.Value;

        var byCategory = await _db.ServiceCatalogItems
            .Where(s => s.IsActive && s.Category.ToLower() == category.ToLower())
            .Select(s => (decimal?)s.UnitPrice)
            .FirstOrDefaultAsync(ct);

        return byCategory ?? 0m;
    }

    /// <summary>Finds a catalog price from a pre-loaded list (avoids N+1 in loops).</summary>
    private static decimal FindCatalogPrice(List<ServiceCatalogItem> catalog, string name, string category)
    {
        var byName = catalog.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (byName is not null) return byName.UnitPrice;

        var byCategory = catalog.FirstOrDefault(s => s.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        return byCategory?.UnitPrice ?? 0m;
    }

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
            seq = last + 1;

        return $"{prefix}{seq:D5}";
    }
}
