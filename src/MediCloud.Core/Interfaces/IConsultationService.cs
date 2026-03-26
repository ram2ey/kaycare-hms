using MediCloud.Core.DTOs.Consultations;

namespace MediCloud.Core.Interfaces;

public interface IConsultationService
{
    Task<ConsultationDetailResponse> CreateAsync(CreateConsultationRequest request, CancellationToken ct = default);
    Task<ConsultationDetailResponse> GetByIdAsync(Guid consultationId, CancellationToken ct = default);
    Task<ConsultationDetailResponse> UpdateAsync(Guid consultationId, UpdateConsultationRequest request, CancellationToken ct = default);
    Task<ConsultationDetailResponse> SignAsync(Guid consultationId, CancellationToken ct = default);
    Task<IReadOnlyList<ConsultationSummaryResponse>> GetPatientHistoryAsync(Guid patientId, CancellationToken ct = default);
    Task<ConsultationDetailResponse?> GetByAppointmentAsync(Guid appointmentId, CancellationToken ct = default);
}
