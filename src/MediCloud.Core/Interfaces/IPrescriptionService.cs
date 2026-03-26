using MediCloud.Core.DTOs.Prescriptions;

namespace MediCloud.Core.Interfaces;

public interface IPrescriptionService
{
    Task<PrescriptionDetailResponse> CreateAsync(CreatePrescriptionRequest request, CancellationToken ct = default);
    Task<PrescriptionDetailResponse> GetByIdAsync(Guid prescriptionId, CancellationToken ct = default);
    Task<PrescriptionDetailResponse> DispenseAsync(Guid prescriptionId, DispensePrescriptionRequest request, CancellationToken ct = default);
    Task<PrescriptionDetailResponse> CancelAsync(Guid prescriptionId, CancellationToken ct = default);
    Task<IReadOnlyList<PrescriptionResponse>> GetPatientHistoryAsync(Guid patientId, CancellationToken ct = default);
    Task<IReadOnlyList<PrescriptionResponse>> GetByConsultationAsync(Guid consultationId, CancellationToken ct = default);
    Task<IReadOnlyList<PrescriptionResponse>> GetPendingAsync(CancellationToken ct = default);
}
