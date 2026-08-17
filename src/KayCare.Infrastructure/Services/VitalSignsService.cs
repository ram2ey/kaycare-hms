using KayCare.Core.Constants;
using KayCare.Core.DTOs.Nursing;
using KayCare.Core.Entities;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KayCare.Infrastructure.Services;

public class VitalSignsService : IVitalSignsService
{
    private readonly AppDbContext        _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService       _audit;
    private readonly ILogger<VitalSignsService> _logger;

    public VitalSignsService(AppDbContext db, ICurrentUserService currentUser, IAuditService audit, ILogger<VitalSignsService> logger)
    {
        _db          = db;
        _currentUser = currentUser;
        _audit       = audit;
        _logger      = logger;
    }

    public async Task<VitalSignsResponse> RecordAsync(Guid patientId, RecordVitalSignsRequest req, CancellationToken ct = default)
    {
        var vs = new VitalSigns
        {
            VitalSignsId           = Guid.NewGuid(),
            PatientId              = patientId,
            AdmissionId            = req.AdmissionId,
            ConsultationId         = req.ConsultationId,
            RecordedByUserId       = _currentUser.UserId,
            RecordedAt             = DateTime.UtcNow,
            BloodPressureSystolic  = req.BloodPressureSystolic,
            BloodPressureDiastolic = req.BloodPressureDiastolic,
            PulseRate              = req.PulseRate,
            Temperature            = req.Temperature,
            SpO2                   = req.SpO2,
            RespiratoryRate        = req.RespiratoryRate,
            Weight                 = req.Weight,
            Height                 = req.Height,
            Notes                  = req.Notes,
        };
        _db.VitalSigns.Add(vs);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditActions.VitalSignsRecord, nameof(VitalSigns), vs.VitalSignsId, vs.PatientId, ct: ct);
        _logger.LogInformation("Vital signs {VitalSignsId} recorded for patient {PatientId}", vs.VitalSignsId, vs.PatientId);

        var recorder = await _db.Users.AsNoTracking().FirstAsync(u => u.UserId == vs.RecordedByUserId, ct);
        return ToResponse(vs, $"{recorder.FirstName} {recorder.LastName}");
    }

    public async Task<VitalSignsResponse?> GetLatestAsync(Guid patientId, CancellationToken ct = default)
    {
        var vs = await _db.VitalSigns
            .Include(v => v.RecordedBy)
            .AsNoTracking()
            .Where(v => v.PatientId == patientId)
            .OrderByDescending(v => v.RecordedAt)
            .FirstOrDefaultAsync(ct);

        return vs is null ? null : ToResponse(vs, $"{vs.RecordedBy.FirstName} {vs.RecordedBy.LastName}");
    }

    public async Task<List<VitalSignsResponse>> GetForPatientAsync(Guid patientId, int limit = 20, CancellationToken ct = default)
    {
        var list = await _db.VitalSigns
            .Include(v => v.RecordedBy)
            .AsNoTracking()
            .Where(v => v.PatientId == patientId)
            .OrderByDescending(v => v.RecordedAt)
            .Take(limit)
            .ToListAsync(ct);

        return list.Select(v => ToResponse(v, $"{v.RecordedBy.FirstName} {v.RecordedBy.LastName}")).ToList();
    }

    public async Task<List<VitalSignsResponse>> GetForAdmissionAsync(Guid admissionId, CancellationToken ct = default)
    {
        var list = await _db.VitalSigns
            .Include(v => v.RecordedBy)
            .AsNoTracking()
            .Where(v => v.AdmissionId == admissionId)
            .OrderByDescending(v => v.RecordedAt)
            .ToListAsync(ct);

        return list.Select(v => ToResponse(v, $"{v.RecordedBy.FirstName} {v.RecordedBy.LastName}")).ToList();
    }

    private static VitalSignsResponse ToResponse(VitalSigns v, string recorderName)
    {
        decimal? bmi = null;
        if (v.Weight.HasValue && v.Height.HasValue && v.Height.Value > 0)
        {
            var heightM = v.Height.Value / 100m;
            bmi = Math.Round(v.Weight.Value / (heightM * heightM), 1);
        }

        return new VitalSignsResponse(
            v.VitalSignsId, v.PatientId, v.AdmissionId, v.ConsultationId,
            recorderName, v.RecordedAt,
            v.BloodPressureSystolic, v.BloodPressureDiastolic,
            v.PulseRate, v.Temperature, v.SpO2, v.RespiratoryRate,
            v.Weight, v.Height, bmi, v.Notes);
    }
}
