using KayCare.Core.Constants;
using KayCare.Core.Entities;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KayCare.Infrastructure.Services;

public class ChargeCaptureService : IChargeCaptureService
{
    private readonly AppDbContext        _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext      _tenantContext;
    private readonly IAuditService       _audit;
    private readonly ILogger<ChargeCaptureService> _logger;

    public ChargeCaptureService(
        AppDbContext db,
        ICurrentUserService currentUser,
        ITenantContext tenantContext,
        IAuditService audit,
        ILogger<ChargeCaptureService> logger)
    {
        _db           = db;
        _currentUser  = currentUser;
        _tenantContext = tenantContext;
        _audit        = audit;
        _logger       = logger;
    }

    // ── Consultation ──────────────────────────────────────────────────────────

    public async Task CaptureConsultationChargeAsync(Guid consultationId, CancellationToken ct = default)
    {
        var ownsTransaction = _db.Database.CurrentTransaction == null;
        var transaction = ownsTransaction ? await _db.Database.BeginTransactionAsync(ct) : null;
        try
        {
            await _db.AcquireAdvisoryLockAsync(_tenantContext.TenantId, "BillNumber", ct);

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
            var price = await _db.ResolvePriceAsync("Medical Consultation", "Consultation", bill.PayerId, ct);

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

            await _audit.LogAsync(AuditActions.ChargeCapture, nameof(Bill), bill.BillId, consultation.PatientId,
                details: $"Consultation charge captured; ConsultationId={consultationId}", ct: ct);
            _logger.LogInformation("Consultation charge captured on bill {BillId} for consultation {ConsultationId}",
                bill.BillId, consultationId);

            if (ownsTransaction && transaction != null)
            {
                await transaction.CommitAsync(ct);
            }
        }
        catch
        {
            if (ownsTransaction && transaction != null)
            {
                await transaction.RollbackAsync(ct);
            }
            throw;
        }
        finally
        {
            if (ownsTransaction && transaction != null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    // ── Lab Order ─────────────────────────────────────────────────────────────

    public async Task CaptureLabOrderChargesAsync(Guid labOrderId, CancellationToken ct = default)
    {
        var ownsTransaction = _db.Database.CurrentTransaction == null;
        var transaction = ownsTransaction ? await _db.Database.BeginTransactionAsync(ct) : null;
        try
        {
            await _db.AcquireAdvisoryLockAsync(_tenantContext.TenantId, "BillNumber", ct);

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

                var price = await _db.ResolvePriceFromCatalogAsync(catalog, labItem.TestName, "Laboratory", bill.PayerId, ct);

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

                await _audit.LogAsync(AuditActions.ChargeCapture, nameof(Bill), bill.BillId, order.PatientId,
                    details: $"Lab order charges captured; LabOrderId={labOrderId}", ct: ct);
                _logger.LogInformation("Lab order charges captured on bill {BillId} for lab order {LabOrderId}",
                    bill.BillId, labOrderId);
            }

            if (ownsTransaction && transaction != null)
            {
                await transaction.CommitAsync(ct);
            }
        }
        catch
        {
            if (ownsTransaction && transaction != null)
            {
                await transaction.RollbackAsync(ct);
            }
            throw;
        }
        finally
        {
            if (ownsTransaction && transaction != null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    // ── Prescription Dispense ─────────────────────────────────────────────────

    public async Task CaptureDispenseChargesAsync(Guid prescriptionId, Guid dispenseEventId, CancellationToken ct = default)
    {
        var ownsTransaction = _db.Database.CurrentTransaction == null;
        var transaction = ownsTransaction ? await _db.Database.BeginTransactionAsync(ct) : null;
        try
        {
            await _db.AcquireAdvisoryLockAsync(_tenantContext.TenantId, "BillNumber", ct);

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
                var price    = await _db.ResolvePriceFromCatalogAsync(catalog, med.MedicationName, "Medication", bill.PayerId, ct);
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

            await _audit.LogAsync(AuditActions.ChargeCapture, nameof(Bill), bill.BillId, prescription.PatientId,
                details: $"Dispense charges captured; PrescriptionId={prescriptionId}; DispenseEventId={dispenseEventId}", ct: ct);
            _logger.LogInformation("Dispense charges captured on bill {BillId} for prescription {PrescriptionId}",
                bill.BillId, prescriptionId);

            if (ownsTransaction && transaction != null)
            {
                await transaction.CommitAsync(ct);
            }
        }
        catch
        {
            if (ownsTransaction && transaction != null)
            {
                await transaction.RollbackAsync(ct);
            }
            throw;
        }
        finally
        {
            if (ownsTransaction && transaction != null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    // ── Shared helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Returns an existing Draft or Issued bill for the consultation, or creates a new Draft bill.
    /// Automatically assigns PayerId if patient has insurance details registered.
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

        var patient = await _db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.PatientId == patientId, ct);
        Guid? payerId = null;
        if (patient != null && !string.IsNullOrWhiteSpace(patient.InsuranceProvider))
        {
            var payer = await _db.Payers.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Name.ToLower().Contains(patient.InsuranceProvider.ToLower()) || patient.InsuranceProvider.ToLower().Contains(p.Name.ToLower()), ct);
            payerId = payer?.PayerId;
        }

        var billNumber = await GenerateBillNumberAsync(ct);
        var bill = new Bill
        {
            BillNumber      = billNumber,
            PatientId       = patientId,
            ConsultationId  = consultationId,
            PayerId         = payerId,
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
