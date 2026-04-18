using KayCare.Core.DTOs.Appointments;

namespace KayCare.Core.Interfaces;

public interface IAppointmentService
{
    Task<AppointmentDetailResponse> CreateAsync(CreateAppointmentRequest request, CancellationToken ct = default);
    Task<AppointmentDetailResponse> GetByIdAsync(Guid appointmentId, CancellationToken ct = default);
    Task<AppointmentDetailResponse> UpdateAsync(Guid appointmentId, UpdateAppointmentRequest request, CancellationToken ct = default);
    Task<AppointmentDetailResponse> TransitionStatusAsync(Guid appointmentId, string targetStatus, string? reason = null, CancellationToken ct = default);
    Task<IReadOnlyList<AppointmentResponse>> GetCalendarAsync(CalendarRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<AppointmentResponse>> GetPatientAppointmentsAsync(Guid patientId, CancellationToken ct = default);
}
