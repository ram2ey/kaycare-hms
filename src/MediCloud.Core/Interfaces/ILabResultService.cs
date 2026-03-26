using MediCloud.Core.DTOs.LabResults;

namespace MediCloud.Core.Interfaces;

public interface ILabResultService
{
    Task<IReadOnlyList<LabResultResponse>> GetByPatientAsync(Guid patientId, CancellationToken ct);
    Task<LabResultDetailResponse?>         GetByAccessionAsync(string accessionNumber, CancellationToken ct);
    Task<LabResultDetailResponse?>         GetByIdAsync(Guid labResultId, CancellationToken ct);
}
