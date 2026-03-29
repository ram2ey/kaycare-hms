using KayCare.Core.Constants;
using KayCare.Core.DTOs.Prescriptions;
using KayCare.Core.Entities;
using KayCare.Core.Exceptions;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KayCare.Infrastructure.Services;

public class PrescriptionService : IPrescriptionService
{
    private readonly AppDbContext          _db;
    private readonly ICurrentUserService   _currentUser;
    private readonly ITenantContext        _tenantContext;
    private readonly IChargeCaptureService _chargeCapture;

    public PrescriptionService(AppDbContext db, ICurrentUserService currentUser, ITenantContext tenantContext, IChargeCaptureService chargeCapture)
    {
        _db            = db;
        _currentUser   = currentUser;
        _tenantContext = tenantContext;
        _chargeCapture = chargeCapture;
    }

    // ── Create ────────────────────────────────────────────────────────────────

    public async Task<PrescriptionDetailResponse> CreateAsync(CreatePrescriptionRequest req, CancellationToken ct = default)
    {
        var consultation = await _db.Consultations
            .FirstOrDefaultAsync(c => c.ConsultationId == req.ConsultationId, ct)
            ?? throw new NotFoundException(nameof(Consultation), req.ConsultationId);

        if (!req.Items.Any())
            throw new AppException("A prescription must contain at least one medication.", 400);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var prescription = new Prescription
        {
            ConsultationId     = req.ConsultationId,
            PatientId          = consultation.PatientId,
            PrescribedByUserId = _currentUser.UserId,
            PrescriptionDate   = today,
            ExpiresAt          = today.AddDays(30),
            Status             = PrescriptionStatus.Active,
            Notes              = req.Notes
        };

        _db.Prescriptions.Add(prescription);
        await _db.SaveChangesAsync(ct); // flush to get PrescriptionId

        var items = req.Items.Select(i => new PrescriptionItem
        {
            PrescriptionId        = prescription.PrescriptionId,
            TenantId              = _tenantContext.TenantId,
            MedicationName        = i.MedicationName.Trim(),
            GenericName           = i.GenericName?.Trim(),
            Strength              = i.Strength.Trim(),
            DosageForm            = i.DosageForm,
            Frequency             = i.Frequency,
            DurationDays          = i.DurationDays,
            Quantity              = i.Quantity,
            Refills               = i.Refills,
            Instructions          = i.Instructions?.Trim(),
            IsControlledSubstance = i.IsControlledSubstance
        }).ToList();

        _db.PrescriptionItems.AddRange(items);
        await _db.SaveChangesAsync(ct);

        return await LoadDetailAsync(prescription.PrescriptionId, ct);
    }

    // ── Get ───────────────────────────────────────────────────────────────────

    public async Task<PrescriptionDetailResponse> GetByIdAsync(Guid prescriptionId, CancellationToken ct = default)
        => await LoadDetailAsync(prescriptionId, ct);

    public async Task<IReadOnlyList<PrescriptionResponse>> GetPatientHistoryAsync(Guid patientId, CancellationToken ct = default)
    {
        var patientExists = await _db.Patients.AnyAsync(p => p.PatientId == patientId, ct);
        if (!patientExists) throw new NotFoundException(nameof(Patient), patientId);

        var rows = await _db.Prescriptions
            .Include(p => p.Patient)
            .Include(p => p.PrescribedBy)
            .Include(p => p.Items)
            .AsNoTracking()
            .Where(p => p.PatientId == patientId)
            .OrderByDescending(p => p.PrescriptionDate)
            .ToListAsync(ct);

        return rows.Select(MapToSummary).ToList();
    }

    public async Task<IReadOnlyList<PrescriptionResponse>> GetByConsultationAsync(Guid consultationId, CancellationToken ct = default)
    {
        var rows = await _db.Prescriptions
            .Include(p => p.Patient)
            .Include(p => p.PrescribedBy)
            .Include(p => p.Items)
            .AsNoTracking()
            .Where(p => p.ConsultationId == consultationId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

        return rows.Select(MapToSummary).ToList();
    }

    public async Task<IReadOnlyList<PrescriptionResponse>> GetPendingAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var rows = await _db.Prescriptions
            .Include(p => p.Patient)
            .Include(p => p.PrescribedBy)
            .Include(p => p.Items)
            .AsNoTracking()
            .Where(p => (p.Status == PrescriptionStatus.Active || p.Status == PrescriptionStatus.PartiallyDispensed)
                     && (p.ExpiresAt == null || p.ExpiresAt >= today))
            .OrderBy(p => p.PrescriptionDate)
            .ThenBy(p => p.CreatedAt)
            .ToListAsync(ct);

        return rows.Select(MapToSummary).ToList();
    }

    // ── Dispense (full) ───────────────────────────────────────────────────────

    public async Task<PrescriptionDetailResponse> DispenseAsync(Guid prescriptionId, DispensePrescriptionRequest req, CancellationToken ct = default)
    {
        var prescription = await _db.Prescriptions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.PrescriptionId == prescriptionId, ct)
            ?? throw new NotFoundException(nameof(Prescription), prescriptionId);

        if (prescription.Status != PrescriptionStatus.Active && prescription.Status != PrescriptionStatus.PartiallyDispensed)
            throw new AppException($"Cannot dispense a prescription with status '{prescription.Status}'.", 400);

        var now = DateTime.UtcNow;

        // Mark all items as fully dispensed
        foreach (var item in prescription.Items)
        {
            item.QuantityDispensed = item.Quantity;
            item.IsFullyDispensed  = true;
        }

        // Create a dispense event for the full dispense
        var dispenseEvent = new DispenseEvent
        {
            TenantId          = _tenantContext.TenantId,
            PrescriptionId    = prescriptionId,
            DispensedByUserId = _currentUser.UserId,
            DispensedAt       = now,
            Notes             = req.Notes,
            Items             = prescription.Items.Select(item => new DispenseEventItem
            {
                TenantId           = _tenantContext.TenantId,
                PrescriptionItemId = item.ItemId,
                QuantityDispensed  = item.Quantity
            }).ToList()
        };

        _db.DispenseEvents.Add(dispenseEvent);

        prescription.Status            = PrescriptionStatus.Dispensed;
        prescription.DispensedAt       = now;
        prescription.DispensedByUserId = _currentUser.UserId;

        if (req.Notes is not null)
            prescription.Notes = string.IsNullOrWhiteSpace(prescription.Notes)
                ? req.Notes
                : $"{prescription.Notes}\n[Dispensing note] {req.Notes}";

        await _db.SaveChangesAsync(ct);

        await _chargeCapture.CaptureDispenseChargesAsync(prescriptionId, dispenseEvent.DispenseEventId, ct);

        return await LoadDetailAsync(prescriptionId, ct);
    }

    // ── Partial Dispense ──────────────────────────────────────────────────────

    public async Task<PrescriptionDetailResponse> PartialDispenseAsync(Guid prescriptionId, PartialDispenseRequest req, CancellationToken ct = default)
    {
        var prescription = await _db.Prescriptions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.PrescriptionId == prescriptionId, ct)
            ?? throw new NotFoundException(nameof(Prescription), prescriptionId);

        if (prescription.Status != PrescriptionStatus.Active && prescription.Status != PrescriptionStatus.PartiallyDispensed)
            throw new AppException($"Cannot dispense a prescription with status '{prescription.Status}'.", 400);

        if (!req.Items.Any(i => i.QuantityToDispense > 0))
            throw new AppException("At least one item must have a quantity to dispense.", 400);

        // Validate each requested item exists and won't be over-dispensed
        var itemMap = prescription.Items.ToDictionary(i => i.ItemId);
        foreach (var reqItem in req.Items.Where(i => i.QuantityToDispense > 0))
        {
            if (!itemMap.TryGetValue(reqItem.PrescriptionItemId, out var item))
                throw new AppException($"Item {reqItem.PrescriptionItemId} does not belong to this prescription.", 400);

            var remaining = item.Quantity - item.QuantityDispensed;
            if (reqItem.QuantityToDispense > remaining)
                throw new AppException($"Cannot dispense {reqItem.QuantityToDispense} of {item.MedicationName}: only {remaining} remaining.", 400);
        }

        var now = DateTime.UtcNow;

        // Create dispense event
        var eventItems = req.Items
            .Where(i => i.QuantityToDispense > 0)
            .Select(i => new DispenseEventItem
            {
                TenantId           = _tenantContext.TenantId,
                PrescriptionItemId = i.PrescriptionItemId,
                QuantityDispensed  = i.QuantityToDispense
            }).ToList();

        var dispenseEvent = new DispenseEvent
        {
            TenantId          = _tenantContext.TenantId,
            PrescriptionId    = prescriptionId,
            DispensedByUserId = _currentUser.UserId,
            DispensedAt       = now,
            Notes             = req.Notes,
            Items             = eventItems
        };

        _db.DispenseEvents.Add(dispenseEvent);

        // Update running totals on each item
        foreach (var reqItem in req.Items.Where(i => i.QuantityToDispense > 0))
        {
            var item = itemMap[reqItem.PrescriptionItemId];
            item.QuantityDispensed += reqItem.QuantityToDispense;
            item.IsFullyDispensed   = item.QuantityDispensed >= item.Quantity;
        }

        // Set prescription status based on whether all items are fully dispensed
        var allFullyDispensed = prescription.Items.All(i => i.IsFullyDispensed);
        if (allFullyDispensed)
        {
            prescription.Status            = PrescriptionStatus.Dispensed;
            prescription.DispensedAt       = now;
            prescription.DispensedByUserId = _currentUser.UserId;
        }
        else
        {
            prescription.Status = PrescriptionStatus.PartiallyDispensed;
        }

        await _db.SaveChangesAsync(ct);

        await _chargeCapture.CaptureDispenseChargesAsync(prescriptionId, dispenseEvent.DispenseEventId, ct);

        return await LoadDetailAsync(prescriptionId, ct);
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    public async Task<PrescriptionDetailResponse> CancelAsync(Guid prescriptionId, CancellationToken ct = default)
    {
        var prescription = await _db.Prescriptions
            .FirstOrDefaultAsync(p => p.PrescriptionId == prescriptionId, ct)
            ?? throw new NotFoundException(nameof(Prescription), prescriptionId);

        if (prescription.Status != PrescriptionStatus.Active && prescription.Status != PrescriptionStatus.PartiallyDispensed)
            throw new AppException($"Cannot cancel a prescription with status '{prescription.Status}'.", 400);

        prescription.Status = PrescriptionStatus.Cancelled;
        await _db.SaveChangesAsync(ct);
        return await LoadDetailAsync(prescriptionId, ct);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<PrescriptionDetailResponse> LoadDetailAsync(Guid prescriptionId, CancellationToken ct)
    {
        var p = await _db.Prescriptions
            .Include(p => p.Patient)
            .Include(p => p.PrescribedBy)
            .Include(p => p.DispensedBy)
            .Include(p => p.Items)
            .Include(p => p.DispenseEvents)
                .ThenInclude(e => e.DispensedBy)
            .Include(p => p.DispenseEvents)
                .ThenInclude(e => e.Items)
                    .ThenInclude(i => i.PrescriptionItem)
            .FirstOrDefaultAsync(p => p.PrescriptionId == prescriptionId, ct)
            ?? throw new NotFoundException(nameof(Prescription), prescriptionId);

        // Auto-expire if Active and past expiry date
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (p.Status == PrescriptionStatus.Active && p.ExpiresAt.HasValue && p.ExpiresAt.Value < today)
        {
            p.Status = PrescriptionStatus.Expired;
            await _db.SaveChangesAsync(ct);
        }

        return MapToDetail(p);
    }

    private static PrescriptionResponse MapToSummary(Prescription p) => new()
    {
        PrescriptionId          = p.PrescriptionId,
        ConsultationId          = p.ConsultationId,
        PatientId               = p.PatientId,
        PatientName             = $"{p.Patient.FirstName} {p.Patient.LastName}".Trim(),
        MedicalRecordNumber     = p.Patient.MedicalRecordNumber,
        PrescribedByUserId      = p.PrescribedByUserId,
        PrescribedByName        = $"{p.PrescribedBy.FirstName} {p.PrescribedBy.LastName}".Trim(),
        PrescriptionDate        = p.PrescriptionDate,
        ExpiresAt               = p.ExpiresAt,
        Status                  = p.Status,
        ItemCount               = p.Items.Count,
        HasControlledSubstances = p.Items.Any(i => i.IsControlledSubstance),
        CreatedAt               = p.CreatedAt
    };

    private static PrescriptionDetailResponse MapToDetail(Prescription p) => new()
    {
        PrescriptionId          = p.PrescriptionId,
        ConsultationId          = p.ConsultationId,
        PatientId               = p.PatientId,
        PatientName             = $"{p.Patient.FirstName} {p.Patient.LastName}".Trim(),
        MedicalRecordNumber     = p.Patient.MedicalRecordNumber,
        PrescribedByUserId      = p.PrescribedByUserId,
        PrescribedByName        = $"{p.PrescribedBy.FirstName} {p.PrescribedBy.LastName}".Trim(),
        PrescriptionDate        = p.PrescriptionDate,
        ExpiresAt               = p.ExpiresAt,
        Status                  = p.Status,
        ItemCount               = p.Items.Count,
        HasControlledSubstances = p.Items.Any(i => i.IsControlledSubstance),
        Notes                   = p.Notes,
        DispensedAt             = p.DispensedAt,
        DispensedByName         = p.DispensedBy is null ? null
                                    : $"{p.DispensedBy.FirstName} {p.DispensedBy.LastName}".Trim(),
        CreatedAt               = p.CreatedAt,
        UpdatedAt               = p.UpdatedAt,
        Items                   = p.Items.Select(i => new PrescriptionItemResponse
        {
            ItemId                = i.ItemId,
            MedicationName        = i.MedicationName,
            GenericName           = i.GenericName,
            Strength              = i.Strength,
            DosageForm            = i.DosageForm,
            Frequency             = i.Frequency,
            DurationDays          = i.DurationDays,
            Quantity              = i.Quantity,
            Refills               = i.Refills,
            Instructions          = i.Instructions,
            IsControlledSubstance = i.IsControlledSubstance,
            QuantityDispensed     = i.QuantityDispensed,
            IsFullyDispensed      = i.IsFullyDispensed
        }).ToList(),
        DispenseHistory = p.DispenseEvents
            .OrderBy(e => e.DispensedAt)
            .Select(e => new DispenseEventResponse
            {
                DispenseEventId = e.DispenseEventId,
                DispensedAt     = e.DispensedAt,
                DispensedByName = $"{e.DispensedBy.FirstName} {e.DispensedBy.LastName}".Trim(),
                Notes           = e.Notes,
                Items           = e.Items.Select(i => new DispenseEventItemResponse
                {
                    PrescriptionItemId = i.PrescriptionItemId,
                    MedicationName     = i.PrescriptionItem.MedicationName,
                    QuantityDispensed  = i.QuantityDispensed
                }).ToList()
            }).ToList()
    };
}
