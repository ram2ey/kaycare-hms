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
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext _tenantContext;

    public PrescriptionService(AppDbContext db, ICurrentUserService currentUser, ITenantContext tenantContext)
    {
        _db = db;
        _currentUser = currentUser;
        _tenantContext = tenantContext;
    }

    // ── Create ────────────────────────────────────────────────────────────────

    public async Task<PrescriptionDetailResponse> CreateAsync(CreatePrescriptionRequest req, CancellationToken ct = default)
    {
        var consultation = await _db.Consultations
            .FirstOrDefaultAsync(c => c.ConsultationId == req.ConsultationId, ct)
            ?? throw new NotFoundException(nameof(Consultation), req.ConsultationId);

        if (!req.Items.Any())
            throw new AppException("A prescription must contain at least one medication.", 400);

        var prescription = new Prescription
        {
            ConsultationId     = req.ConsultationId,
            PatientId          = consultation.PatientId,
            PrescribedByUserId = _currentUser.UserId,
            PrescriptionDate   = DateOnly.FromDateTime(DateTime.UtcNow),
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
        var rows = await _db.Prescriptions
            .Include(p => p.Patient)
            .Include(p => p.PrescribedBy)
            .Include(p => p.Items)
            .AsNoTracking()
            .Where(p => p.Status == PrescriptionStatus.Active)
            .OrderBy(p => p.PrescriptionDate)
            .ThenBy(p => p.CreatedAt)
            .ToListAsync(ct);

        return rows.Select(MapToSummary).ToList();
    }

    // ── Dispense ──────────────────────────────────────────────────────────────

    public async Task<PrescriptionDetailResponse> DispenseAsync(Guid prescriptionId, DispensePrescriptionRequest req, CancellationToken ct = default)
    {
        var prescription = await _db.Prescriptions
            .FirstOrDefaultAsync(p => p.PrescriptionId == prescriptionId, ct)
            ?? throw new NotFoundException(nameof(Prescription), prescriptionId);

        if (prescription.Status != PrescriptionStatus.Active)
            throw new AppException($"Cannot dispense a prescription with status '{prescription.Status}'.", 400);

        prescription.Status            = PrescriptionStatus.Dispensed;
        prescription.DispensedAt       = DateTime.UtcNow;
        prescription.DispensedByUserId = _currentUser.UserId;

        if (req.Notes is not null)
            prescription.Notes = string.IsNullOrWhiteSpace(prescription.Notes)
                ? req.Notes
                : $"{prescription.Notes}\n[Dispensing note] {req.Notes}";

        await _db.SaveChangesAsync(ct);
        return await LoadDetailAsync(prescriptionId, ct);
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    public async Task<PrescriptionDetailResponse> CancelAsync(Guid prescriptionId, CancellationToken ct = default)
    {
        var prescription = await _db.Prescriptions
            .FirstOrDefaultAsync(p => p.PrescriptionId == prescriptionId, ct)
            ?? throw new NotFoundException(nameof(Prescription), prescriptionId);

        if (prescription.Status != PrescriptionStatus.Active)
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
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PrescriptionId == prescriptionId, ct)
            ?? throw new NotFoundException(nameof(Prescription), prescriptionId);

        return MapToDetail(p);
    }

    private static PrescriptionResponse MapToSummary(Prescription p) => new()
    {
        PrescriptionId      = p.PrescriptionId,
        ConsultationId      = p.ConsultationId,
        PatientId           = p.PatientId,
        PatientName         = $"{p.Patient.FirstName} {p.Patient.LastName}".Trim(),
        MedicalRecordNumber = p.Patient.MedicalRecordNumber,
        PrescribedByUserId  = p.PrescribedByUserId,
        PrescribedByName    = $"{p.PrescribedBy.FirstName} {p.PrescribedBy.LastName}".Trim(),
        PrescriptionDate    = p.PrescriptionDate,
        Status              = p.Status,
        ItemCount           = p.Items.Count,
        HasControlledSubstances = p.Items.Any(i => i.IsControlledSubstance),
        CreatedAt           = p.CreatedAt
    };

    private static PrescriptionDetailResponse MapToDetail(Prescription p) => new()
    {
        PrescriptionId      = p.PrescriptionId,
        ConsultationId      = p.ConsultationId,
        PatientId           = p.PatientId,
        PatientName         = $"{p.Patient.FirstName} {p.Patient.LastName}".Trim(),
        MedicalRecordNumber = p.Patient.MedicalRecordNumber,
        PrescribedByUserId  = p.PrescribedByUserId,
        PrescribedByName    = $"{p.PrescribedBy.FirstName} {p.PrescribedBy.LastName}".Trim(),
        PrescriptionDate    = p.PrescriptionDate,
        Status              = p.Status,
        ItemCount           = p.Items.Count,
        HasControlledSubstances = p.Items.Any(i => i.IsControlledSubstance),
        Notes               = p.Notes,
        DispensedAt         = p.DispensedAt,
        DispensedByName     = p.DispensedBy is null ? null
                                : $"{p.DispensedBy.FirstName} {p.DispensedBy.LastName}".Trim(),
        CreatedAt           = p.CreatedAt,
        UpdatedAt           = p.UpdatedAt,
        Items               = p.Items.Select(i => new PrescriptionItemResponse
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
            IsControlledSubstance = i.IsControlledSubstance
        }).ToList()
    };
}
