using KayCare.Core.DTOs.Inpatient;

namespace KayCare.Core.Interfaces;

public interface IAdmissionService
{
    Task<List<AdmissionSummaryResponse>> GetAllAsync(string? status = null, Guid? wardId = null, Guid? patientId = null, CancellationToken ct = default);
    Task<AdmissionDetailResponse?>       GetByIdAsync(Guid admissionId, CancellationToken ct = default);
    Task<List<AdmissionSummaryResponse>> GetPatientHistoryAsync(Guid patientId, CancellationToken ct = default);
    Task<AdmissionDetailResponse>        AdmitAsync(AdmitPatientRequest request, CancellationToken ct = default);
    Task<AdmissionDetailResponse>        DischargeAsync(Guid admissionId, DischargePatientRequest request, CancellationToken ct = default);
    Task<AdmissionDetailResponse>        TransferAsync(Guid admissionId, TransferPatientRequest request, CancellationToken ct = default);
    Task<DischargeSummaryResponse>       GetDischargeSummaryAsync(Guid admissionId, CancellationToken ct = default);
    Task<DischargeSummaryResponse>       UpdateDischargeSummaryAsync(Guid admissionId, UpdateDischargeSummaryRequest request, CancellationToken ct = default);
}
