using MediCloud.Core.Constants;
using MediCloud.Core.DTOs.Appointments;
using MediCloud.Core.Entities;
using MediCloud.Core.Exceptions;
using MediCloud.Core.Interfaces;
using MediCloud.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MediCloud.Infrastructure.Services;

public class AppointmentService : IAppointmentService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AppointmentService(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    // ── Create ────────────────────────────────────────────────────────────────

    public async Task<AppointmentDetailResponse> CreateAsync(CreateAppointmentRequest req, CancellationToken ct = default)
    {
        var patientExists = await _db.Patients.AnyAsync(p => p.PatientId == req.PatientId, ct);
        if (!patientExists) throw new NotFoundException(nameof(Patient), req.PatientId);

        var doctorExists = await _db.Users
            .AnyAsync(u => u.UserId == req.DoctorUserId && u.IsActive, ct);
        if (!doctorExists) throw new NotFoundException("Doctor", req.DoctorUserId);

        await CheckDoctorAvailabilityAsync(req.DoctorUserId, req.ScheduledAt, req.DurationMinutes, null, ct);

        var appointment = new Appointment
        {
            PatientId       = req.PatientId,
            DoctorUserId    = req.DoctorUserId,
            ScheduledAt     = req.ScheduledAt,
            DurationMinutes = req.DurationMinutes,
            AppointmentType = req.AppointmentType,
            Status          = AppointmentStatus.Scheduled,
            ChiefComplaint  = req.ChiefComplaint,
            Room            = req.Room,
            Notes           = req.Notes,
            CreatedByUserId = _currentUser.UserId
        };

        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync(ct);

        return await LoadDetailAsync(appointment.AppointmentId, ct);
    }

    // ── Get ───────────────────────────────────────────────────────────────────

    public async Task<AppointmentDetailResponse> GetByIdAsync(Guid appointmentId, CancellationToken ct = default)
        => await LoadDetailAsync(appointmentId, ct);

    // ── Update ────────────────────────────────────────────────────────────────

    public async Task<AppointmentDetailResponse> UpdateAsync(Guid appointmentId, UpdateAppointmentRequest req, CancellationToken ct = default)
    {
        var appt = await _db.Appointments
            .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId, ct)
            ?? throw new NotFoundException(nameof(Appointment), appointmentId);

        if (appt.Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled or AppointmentStatus.NoShow)
            throw new AppException($"Cannot update a {appt.Status} appointment.", 400);

        var newDoctor    = req.DoctorUserId ?? appt.DoctorUserId;
        var newTime      = req.ScheduledAt ?? appt.ScheduledAt;
        var newDuration  = req.DurationMinutes ?? appt.DurationMinutes;

        // Only check availability if time or doctor changed
        if (req.ScheduledAt.HasValue || req.DoctorUserId.HasValue)
            await CheckDoctorAvailabilityAsync(newDoctor, newTime, newDuration, appointmentId, ct);

        if (req.DoctorUserId.HasValue)    appt.DoctorUserId    = req.DoctorUserId.Value;
        if (req.ScheduledAt.HasValue)     appt.ScheduledAt     = req.ScheduledAt.Value;
        if (req.DurationMinutes.HasValue) appt.DurationMinutes = req.DurationMinutes.Value;
        if (req.AppointmentType is not null) appt.AppointmentType = req.AppointmentType;
        if (req.ChiefComplaint is not null)  appt.ChiefComplaint  = req.ChiefComplaint;
        if (req.Room is not null)            appt.Room            = req.Room;
        if (req.Notes is not null)           appt.Notes           = req.Notes;

        await _db.SaveChangesAsync(ct);
        return await LoadDetailAsync(appointmentId, ct);
    }

    // ── Status Transitions ────────────────────────────────────────────────────

    public async Task<AppointmentDetailResponse> TransitionStatusAsync(
        Guid appointmentId, string targetStatus, string? reason = null, CancellationToken ct = default)
    {
        var appt = await _db.Appointments
            .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId, ct)
            ?? throw new NotFoundException(nameof(Appointment), appointmentId);

        if (!AppointmentStatus.CanTransition(appt.Status, targetStatus))
            throw new AppException(
                $"Cannot transition from '{appt.Status}' to '{targetStatus}'.", 409);

        appt.Status = targetStatus;

        if (targetStatus == AppointmentStatus.Cancelled)
        {
            appt.CancelledAt       = DateTime.UtcNow;
            appt.CancelledByUserId = _currentUser.UserId;
            appt.CancellationReason = reason;
        }

        await _db.SaveChangesAsync(ct);
        return await LoadDetailAsync(appointmentId, ct);
    }

    // ── Calendar ──────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<AppointmentResponse>> GetCalendarAsync(CalendarRequest req, CancellationToken ct = default)
    {
        var from = (req.From ?? DateTime.UtcNow.Date).ToUniversalTime();
        var to   = (req.To   ?? from.AddDays(7)).ToUniversalTime();

        var query = _db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .AsNoTracking()
            .Where(a => a.ScheduledAt >= from && a.ScheduledAt < to);

        if (req.DoctorUserId.HasValue)
            query = query.Where(a => a.DoctorUserId == req.DoctorUserId.Value);

        if (!string.IsNullOrEmpty(req.Status))
            query = query.Where(a => a.Status == req.Status);

        var rows = await query.OrderBy(a => a.ScheduledAt).ToListAsync(ct);
        return rows.Select(MapToResponse).ToList();
    }

    // ── Patient Appointments ──────────────────────────────────────────────────

    public async Task<IReadOnlyList<AppointmentResponse>> GetPatientAppointmentsAsync(Guid patientId, CancellationToken ct = default)
    {
        var patientExists = await _db.Patients.AnyAsync(p => p.PatientId == patientId, ct);
        if (!patientExists) throw new NotFoundException(nameof(Patient), patientId);

        var rows = await _db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .AsNoTracking()
            .Where(a => a.PatientId == patientId)
            .OrderByDescending(a => a.ScheduledAt)
            .ToListAsync(ct);

        return rows.Select(MapToResponse).ToList();
    }

    // ── Availability Check ────────────────────────────────────────────────────

    private async Task CheckDoctorAvailabilityAsync(
        Guid doctorUserId, DateTime scheduledAt, int durationMinutes,
        Guid? excludeId, CancellationToken ct)
    {
        var dayStart = scheduledAt.Date;
        var endTime  = scheduledAt.AddMinutes(durationMinutes);

        var sameDay = await _db.Appointments
            .Where(a => a.DoctorUserId == doctorUserId &&
                        a.ScheduledAt >= dayStart &&
                        a.ScheduledAt < dayStart.AddDays(1) &&
                        a.Status != AppointmentStatus.Cancelled &&
                        a.Status != AppointmentStatus.NoShow &&
                        (excludeId == null || a.AppointmentId != excludeId))
            .ToListAsync(ct);

        var conflict = sameDay.Any(a =>
            a.ScheduledAt < endTime &&
            a.ScheduledAt.AddMinutes(a.DurationMinutes) > scheduledAt);

        if (conflict)
            throw new AppException("Doctor already has an appointment during this time slot.", 409);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<AppointmentDetailResponse> LoadDetailAsync(Guid appointmentId, CancellationToken ct)
    {
        var appt = await _db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId, ct)
            ?? throw new NotFoundException(nameof(Appointment), appointmentId);

        return MapToDetail(appt);
    }

    private static AppointmentResponse MapToResponse(Appointment a) => new()
    {
        AppointmentId       = a.AppointmentId,
        PatientId           = a.PatientId,
        PatientName         = $"{a.Patient.FirstName} {a.Patient.LastName}".Trim(),
        MedicalRecordNumber = a.Patient.MedicalRecordNumber,
        DoctorUserId        = a.DoctorUserId,
        DoctorName          = $"{a.Doctor.FirstName} {a.Doctor.LastName}".Trim(),
        ScheduledAt         = a.ScheduledAt,
        DurationMinutes     = a.DurationMinutes,
        AppointmentType     = a.AppointmentType,
        Status              = a.Status,
        ChiefComplaint      = a.ChiefComplaint,
        Room                = a.Room
    };

    private static AppointmentDetailResponse MapToDetail(Appointment a) => new()
    {
        AppointmentId       = a.AppointmentId,
        PatientId           = a.PatientId,
        PatientName         = $"{a.Patient.FirstName} {a.Patient.LastName}".Trim(),
        MedicalRecordNumber = a.Patient.MedicalRecordNumber,
        DoctorUserId        = a.DoctorUserId,
        DoctorName          = $"{a.Doctor.FirstName} {a.Doctor.LastName}".Trim(),
        ScheduledAt         = a.ScheduledAt,
        DurationMinutes     = a.DurationMinutes,
        AppointmentType     = a.AppointmentType,
        Status              = a.Status,
        ChiefComplaint      = a.ChiefComplaint,
        Room                = a.Room,
        Notes               = a.Notes,
        CancelledAt         = a.CancelledAt,
        CancellationReason  = a.CancellationReason,
        CreatedAt           = a.CreatedAt,
        UpdatedAt           = a.UpdatedAt
    };
}
