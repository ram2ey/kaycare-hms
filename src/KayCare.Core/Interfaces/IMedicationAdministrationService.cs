using KayCare.Core.DTOs.Nursing;

namespace KayCare.Core.Interfaces;

public interface IMedicationAdministrationService
{
    Task<MAREntryResponse>      RecordAsync(RecordAdministrationRequest request, CancellationToken ct = default);
    Task<List<MARItemResponse>> GetMARForPatientAsync(Guid patientId, CancellationToken ct = default);
    Task<List<MARItemResponse>> GetMARForAdmissionAsync(Guid admissionId, CancellationToken ct = default);
}
