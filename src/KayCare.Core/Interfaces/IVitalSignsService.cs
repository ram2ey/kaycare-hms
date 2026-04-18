using KayCare.Core.DTOs.Nursing;

namespace KayCare.Core.Interfaces;

public interface IVitalSignsService
{
    Task<VitalSignsResponse>       RecordAsync(Guid patientId, RecordVitalSignsRequest request, CancellationToken ct = default);
    Task<VitalSignsResponse?>      GetLatestAsync(Guid patientId, CancellationToken ct = default);
    Task<List<VitalSignsResponse>> GetForPatientAsync(Guid patientId, int limit = 20, CancellationToken ct = default);
    Task<List<VitalSignsResponse>> GetForAdmissionAsync(Guid admissionId, CancellationToken ct = default);
}
