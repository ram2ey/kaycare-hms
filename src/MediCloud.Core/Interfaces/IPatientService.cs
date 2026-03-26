using MediCloud.Core.DTOs.Common;
using MediCloud.Core.DTOs.Patients;

namespace MediCloud.Core.Interfaces;

public interface IPatientService
{
    Task<PatientDetailResponse> RegisterAsync(CreatePatientRequest request, CancellationToken ct = default);
    Task<PagedResult<PatientResponse>> SearchAsync(PatientSearchRequest request, CancellationToken ct = default);
    Task<PatientDetailResponse> GetByIdAsync(Guid patientId, CancellationToken ct = default);
    Task<PatientDetailResponse> UpdateAsync(Guid patientId, UpdatePatientRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<AllergyResponse>> GetAllergiesAsync(Guid patientId, CancellationToken ct = default);
    Task<AllergyResponse> AddAllergyAsync(Guid patientId, AddAllergyRequest request, CancellationToken ct = default);
    Task RemoveAllergyAsync(Guid patientId, Guid allergyId, CancellationToken ct = default);
}
