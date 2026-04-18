using KayCare.Core.DTOs.LabResults;
using KayCare.Core.Entities;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KayCare.Infrastructure.Services;

public class LabResultService : ILabResultService
{
    private readonly AppDbContext _db;

    public LabResultService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<LabResultResponse>> GetByPatientAsync(
        Guid patientId, CancellationToken ct)
    {
        var results = await _db.LabResults
            .Include(r => r.Patient)
            .Include(r => r.OrderingDoctor)
            .Include(r => r.Observations)
            .Where(r => r.PatientId == patientId)
            .OrderByDescending(r => r.ReceivedAt)
            .AsNoTracking()
            .ToListAsync(ct);

        return results.ConvertAll(ToResponse).AsReadOnly();
    }

    public async Task<LabResultDetailResponse?> GetByAccessionAsync(
        string accessionNumber, CancellationToken ct)
    {
        var result = await _db.LabResults
            .Include(r => r.Patient)
            .Include(r => r.OrderingDoctor)
            .Include(r => r.Observations.OrderBy(o => o.SequenceNumber))
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.AccessionNumber == accessionNumber, ct);

        return result == null ? null : ToDetailResponse(result);
    }

    public async Task<LabResultDetailResponse?> GetByIdAsync(
        Guid labResultId, CancellationToken ct)
    {
        var result = await _db.LabResults
            .Include(r => r.Patient)
            .Include(r => r.OrderingDoctor)
            .Include(r => r.Observations.OrderBy(o => o.SequenceNumber))
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.LabResultId == labResultId, ct);

        return result == null ? null : ToDetailResponse(result);
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static LabResultResponse ToResponse(LabResult r) => new()
    {
        LabResultId          = r.LabResultId,
        PatientId            = r.PatientId,
        PatientMrn           = r.Patient.MedicalRecordNumber,
        PatientName          = $"{r.Patient.FirstName} {r.Patient.LastName}",
        OrderingDoctorUserId = r.OrderingDoctorUserId,
        OrderingDoctorName   = r.OrderingDoctor == null
            ? null
            : $"{r.OrderingDoctor.FirstName} {r.OrderingDoctor.LastName}",
        AccessionNumber      = r.AccessionNumber,
        OrderCode            = r.OrderCode,
        OrderName            = r.OrderName,
        OrderedAt            = r.OrderedAt,
        ReceivedAt           = r.ReceivedAt,
        Status               = r.Status,
        ObservationCount     = r.Observations.Count,
        CreatedAt            = r.CreatedAt,
    };

    private static LabResultDetailResponse ToDetailResponse(LabResult r) => new()
    {
        LabResultId          = r.LabResultId,
        PatientId            = r.PatientId,
        PatientMrn           = r.Patient.MedicalRecordNumber,
        PatientName          = $"{r.Patient.FirstName} {r.Patient.LastName}",
        OrderingDoctorUserId = r.OrderingDoctorUserId,
        OrderingDoctorName   = r.OrderingDoctor == null
            ? null
            : $"{r.OrderingDoctor.FirstName} {r.OrderingDoctor.LastName}",
        AccessionNumber      = r.AccessionNumber,
        OrderCode            = r.OrderCode,
        OrderName            = r.OrderName,
        OrderedAt            = r.OrderedAt,
        ReceivedAt           = r.ReceivedAt,
        Status               = r.Status,
        ObservationCount     = r.Observations.Count,
        CreatedAt            = r.CreatedAt,
        Observations         = r.Observations
            .Select(o => new LabObservationResponse
            {
                LabObservationId = o.LabObservationId,
                SequenceNumber   = o.SequenceNumber,
                TestCode         = o.TestCode,
                TestName         = o.TestName,
                Value            = o.Value,
                Units            = o.Units,
                ReferenceRange   = o.ReferenceRange,
                AbnormalFlag     = o.AbnormalFlag,
            })
            .ToList()
            .AsReadOnly(),
    };
}
