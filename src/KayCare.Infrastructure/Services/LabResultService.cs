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
            .Include(r => r.LabOrderItem)
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
            .Include(r => r.LabOrderItem)
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
            .Include(r => r.LabOrderItem)
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
        LabOrderId           = r.LabOrderItem?.LabOrderId,
        RawHl7               = r.RawHl7,
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
        LabOrderId           = r.LabOrderItem?.LabOrderId,
        RawHl7               = r.RawHl7,
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

    public async Task<bool> ProcessHl7MessageAsync(string rawMessage, CancellationToken ct)
    {
        var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.IsActive, ct);
        if (tenant == null) return false;

        var parsed = Hl7Parser.ParseOruR01(rawMessage);
        if (parsed == null) return false;

        // Try to match to an open LabOrderItem by AccessionNumber
        var orderItem = await _db.LabOrderItems
            .FirstOrDefaultAsync(i => i.AccessionNumber == parsed.AccessionNumber
                && i.TenantId == tenant.TenantId, ct);

        Guid? patientId          = null;
        Guid? orderingDoctorId   = null;

        if (orderItem != null)
        {
            var order = await _db.LabOrders.AsNoTracking()
                .FirstOrDefaultAsync(o => o.LabOrderId == orderItem.LabOrderId, ct);
            if (order != null)
            {
                patientId        = order.PatientId;
                orderingDoctorId = order.OrderingDoctorUserId;
            }
        }

        // If we still have no patient, try MRN lookup from PID segment
        if (patientId == null && !string.IsNullOrEmpty(parsed.PatientMrn))
        {
            var patient = await _db.Patients.AsNoTracking()
                .FirstOrDefaultAsync(p => p.MedicalRecordNumber == parsed.PatientMrn
                    && p.TenantId == tenant.TenantId, ct);
            patientId = patient?.PatientId;
        }

        if (patientId == null) return false;

        // Check for duplicate accession
        var existing = await _db.LabResults
            .FirstOrDefaultAsync(r => r.AccessionNumber == parsed.AccessionNumber
                && r.TenantId == tenant.TenantId, ct);

        if (existing != null) return false;

        var result = new LabResult
        {
            LabResultId          = Guid.NewGuid(),
            PatientId            = patientId.Value,
            OrderingDoctorUserId = orderingDoctorId,
            AccessionNumber      = parsed.AccessionNumber,
            OrderCode            = parsed.OrderCode,
            OrderName            = parsed.OrderName,
            OrderedAt            = parsed.OrderedAt,
            ReceivedAt           = DateTime.UtcNow,
            Status               = Core.Constants.LabResultStatus.Received,
            RawHl7               = rawMessage,
            LabOrderItemId       = orderItem?.LabOrderItemId,
            TenantId             = tenant.TenantId,
        };
        _db.LabResults.Add(result);
        await _db.SaveChangesAsync(ct);

        var hasCritical = false;
        foreach (var obs in parsed.Observations)
        {
            var isObsCritical = false;
            var catalogItem = await _db.LabTestCatalog
                .FirstOrDefaultAsync(t => t.TestCode == obs.TestCode, ct);
            if (catalogItem != null && !string.IsNullOrWhiteSpace(catalogItem.CriticalReferenceRange))
            {
                isObsCritical = LabOrderService.IsValueCritical(obs.Value, catalogItem.CriticalReferenceRange);
                if (isObsCritical)
                {
                    hasCritical = true;
                }
            }

            _db.LabObservations.Add(new LabObservation
            {
                LabObservationId = Guid.NewGuid(),
                LabResultId    = result.LabResultId,
                TenantId       = tenant.TenantId,
                SequenceNumber = obs.SequenceNumber > 0 ? obs.SequenceNumber : 1,
                TestCode       = obs.TestCode,
                TestName       = obs.TestName,
                Value          = obs.Value,
                Units          = obs.Units,
                ReferenceRange = obs.ReferenceRange,
                AbnormalFlag   = obs.AbnormalFlag,
            });
        }
        await _db.SaveChangesAsync(ct);

        // Update linked LabOrderItem status
        if (orderItem != null)
        {
            orderItem.LabResultId = result.LabResultId;
            orderItem.Status      = Core.Constants.LabOrderItemStatus.Resulted;
            orderItem.ResultedAt  = DateTime.UtcNow;
            orderItem.IsCritical  = hasCritical;
            await _db.SaveChangesAsync(ct);
        }

        return true;
    }
}
