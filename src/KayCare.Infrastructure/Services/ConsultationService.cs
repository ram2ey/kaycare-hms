using System.Text.Json;
using KayCare.Core.Constants;
using KayCare.Core.DTOs.Consultations;
using KayCare.Core.Entities;
using KayCare.Core.Exceptions;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KayCare.Infrastructure.Services;

public class ConsultationService : IConsultationService
{
    private readonly AppDbContext          _db;
    private readonly ICurrentUserService   _currentUser;
    private readonly IChargeCaptureService _chargeCapture;

    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    public ConsultationService(AppDbContext db, ICurrentUserService currentUser, IChargeCaptureService chargeCapture)
    {
        _db            = db;
        _currentUser   = currentUser;
        _chargeCapture = chargeCapture;
    }

    // ── Create ────────────────────────────────────────────────────────────────

    public async Task<ConsultationDetailResponse> CreateAsync(CreateConsultationRequest req, CancellationToken ct = default)
    {
        var appointment = await _db.Appointments
            .FirstOrDefaultAsync(a => a.AppointmentId == req.AppointmentId, ct)
            ?? throw new NotFoundException(nameof(Appointment), req.AppointmentId);

        var duplicate = await _db.Consultations
            .AnyAsync(c => c.AppointmentId == req.AppointmentId, ct);
        if (duplicate)
            throw new AppException("A consultation already exists for this appointment.", 409);

        var consultation = new Consultation
        {
            AppointmentId  = req.AppointmentId,
            PatientId      = appointment.PatientId,
            DoctorUserId   = appointment.DoctorUserId,
            SubjectiveNotes  = req.SubjectiveNotes,
            ObjectiveNotes   = req.ObjectiveNotes,
            AssessmentNotes  = req.AssessmentNotes,
            PlanNotes        = req.PlanNotes,
            Status           = ConsultationStatus.Draft
        };

        _db.Consultations.Add(consultation);

        // Auto-advance appointment to InProgress if in a valid state
        if (AppointmentStatus.CanTransition(appointment.Status, AppointmentStatus.InProgress))
            appointment.Status = AppointmentStatus.InProgress;

        await _db.SaveChangesAsync(ct);
        return await LoadDetailAsync(consultation.ConsultationId, ct);
    }

    // ── Get ───────────────────────────────────────────────────────────────────

    public async Task<ConsultationDetailResponse> GetByIdAsync(Guid consultationId, CancellationToken ct = default)
        => await LoadDetailAsync(consultationId, ct);

    public async Task<ConsultationDetailResponse?> GetByAppointmentAsync(Guid appointmentId, CancellationToken ct = default)
    {
        var c = await _db.Consultations
            .Include(c => c.Patient)
            .Include(c => c.Doctor)
            .Include(c => c.Appointment)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.AppointmentId == appointmentId, ct);

        return c is null ? null : MapToDetail(c);
    }

    public async Task<IReadOnlyList<ConsultationSummaryResponse>> GetPatientHistoryAsync(Guid patientId, CancellationToken ct = default)
    {
        var patientExists = await _db.Patients.AnyAsync(p => p.PatientId == patientId, ct);
        if (!patientExists) throw new NotFoundException(nameof(Patient), patientId);

        var rows = await _db.Consultations
            .Include(c => c.Patient)
            .Include(c => c.Doctor)
            .AsNoTracking()
            .Where(c => c.PatientId == patientId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

        return rows.Select(MapToSummary).ToList();
    }

    // ── Update ────────────────────────────────────────────────────────────────

    public async Task<ConsultationDetailResponse> UpdateAsync(Guid consultationId, UpdateConsultationRequest req, CancellationToken ct = default)
    {
        var consultation = await _db.Consultations
            .FirstOrDefaultAsync(c => c.ConsultationId == consultationId, ct)
            ?? throw new NotFoundException(nameof(Consultation), consultationId);

        if (consultation.Status == ConsultationStatus.Signed)
            throw new AppException("Signed consultations cannot be edited.", 400);

        if (req.SubjectiveNotes is not null)  consultation.SubjectiveNotes  = req.SubjectiveNotes;
        if (req.ObjectiveNotes  is not null)  consultation.ObjectiveNotes   = req.ObjectiveNotes;
        if (req.AssessmentNotes is not null)  consultation.AssessmentNotes  = req.AssessmentNotes;
        if (req.PlanNotes       is not null)  consultation.PlanNotes        = req.PlanNotes;

        if (req.BloodPressureSystolic.HasValue)  consultation.BloodPressureSystolic  = req.BloodPressureSystolic;
        if (req.BloodPressureDiastolic.HasValue) consultation.BloodPressureDiastolic = req.BloodPressureDiastolic;
        if (req.HeartRateBPM.HasValue)           consultation.HeartRateBPM           = req.HeartRateBPM;
        if (req.TemperatureCelsius.HasValue)     consultation.TemperatureCelsius     = req.TemperatureCelsius;
        if (req.WeightKg.HasValue)               consultation.WeightKg               = req.WeightKg;
        if (req.HeightCm.HasValue)               consultation.HeightCm               = req.HeightCm;
        if (req.OxygenSaturationPct.HasValue)    consultation.OxygenSaturationPct    = req.OxygenSaturationPct;

        if (req.PrimaryDiagnosisCode is not null) consultation.PrimaryDiagnosisCode = req.PrimaryDiagnosisCode;
        if (req.PrimaryDiagnosisDesc is not null) consultation.PrimaryDiagnosisDesc = req.PrimaryDiagnosisDesc;

        if (req.SecondaryDiagnoses is not null)
            consultation.SecondaryDiagnoses = JsonSerializer.Serialize(req.SecondaryDiagnoses, JsonOpts);

        await _db.SaveChangesAsync(ct);
        return await LoadDetailAsync(consultationId, ct);
    }

    // ── Sign-off ──────────────────────────────────────────────────────────────

    public async Task<ConsultationDetailResponse> SignAsync(Guid consultationId, CancellationToken ct = default)
    {
        var consultation = await _db.Consultations
            .FirstOrDefaultAsync(c => c.ConsultationId == consultationId, ct)
            ?? throw new NotFoundException(nameof(Consultation), consultationId);

        if (consultation.Status == ConsultationStatus.Signed)
            throw new AppException("Consultation is already signed.", 400);

        if (consultation.DoctorUserId != _currentUser.UserId &&
            _currentUser.Role is not (Roles.SuperAdmin or Roles.Admin))
            throw new AppException("Only the consulting doctor can sign off this consultation.", 403);

        consultation.Status   = ConsultationStatus.Signed;
        consultation.SignedAt = DateTime.UtcNow;

        // Auto-complete the appointment when the doctor signs off
        var appointment = await _db.Appointments
            .FirstOrDefaultAsync(a => a.AppointmentId == consultation.AppointmentId, ct);
        if (appointment is not null &&
            AppointmentStatus.CanTransition(appointment.Status, AppointmentStatus.Completed))
            appointment.Status = AppointmentStatus.Completed;

        await _db.SaveChangesAsync(ct);

        await _chargeCapture.CaptureConsultationChargeAsync(consultationId, ct);

        return await LoadDetailAsync(consultationId, ct);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<ConsultationDetailResponse> LoadDetailAsync(Guid consultationId, CancellationToken ct)
    {
        var c = await _db.Consultations
            .Include(c => c.Patient)
            .Include(c => c.Doctor)
            .Include(c => c.Appointment)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ConsultationId == consultationId, ct)
            ?? throw new NotFoundException(nameof(Consultation), consultationId);

        return MapToDetail(c);
    }

    private static ConsultationSummaryResponse MapToSummary(Consultation c) => new()
    {
        ConsultationId       = c.ConsultationId,
        AppointmentId        = c.AppointmentId,
        PatientId            = c.PatientId,
        PatientName          = $"{c.Patient.FirstName} {c.Patient.LastName}".Trim(),
        MedicalRecordNumber  = c.Patient.MedicalRecordNumber,
        DoctorUserId         = c.DoctorUserId,
        DoctorName           = $"{c.Doctor.FirstName} {c.Doctor.LastName}".Trim(),
        PrimaryDiagnosisCode = c.PrimaryDiagnosisCode,
        PrimaryDiagnosisDesc = c.PrimaryDiagnosisDesc,
        Status               = c.Status,
        SignedAt             = c.SignedAt,
        CreatedAt            = c.CreatedAt
    };

    private static ConsultationDetailResponse MapToDetail(Consultation c) => new()
    {
        ConsultationId       = c.ConsultationId,
        AppointmentId        = c.AppointmentId,
        PatientId            = c.PatientId,
        PatientName          = $"{c.Patient.FirstName} {c.Patient.LastName}".Trim(),
        MedicalRecordNumber  = c.Patient.MedicalRecordNumber,
        DoctorUserId         = c.DoctorUserId,
        DoctorName           = $"{c.Doctor.FirstName} {c.Doctor.LastName}".Trim(),
        SubjectiveNotes      = c.SubjectiveNotes,
        ObjectiveNotes       = c.ObjectiveNotes,
        AssessmentNotes      = c.AssessmentNotes,
        PlanNotes            = c.PlanNotes,
        BloodPressureSystolic  = c.BloodPressureSystolic,
        BloodPressureDiastolic = c.BloodPressureDiastolic,
        HeartRateBPM           = c.HeartRateBPM,
        TemperatureCelsius     = c.TemperatureCelsius,
        WeightKg               = c.WeightKg,
        HeightCm               = c.HeightCm,
        OxygenSaturationPct    = c.OxygenSaturationPct,
        PrimaryDiagnosisCode   = c.PrimaryDiagnosisCode,
        PrimaryDiagnosisDesc   = c.PrimaryDiagnosisDesc,
        SecondaryDiagnoses     = JsonSerializer.Deserialize<List<DiagnosisDto>>(
                                     c.SecondaryDiagnoses, JsonOpts) ?? [],
        Status    = c.Status,
        SignedAt  = c.SignedAt,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt
    };
}
