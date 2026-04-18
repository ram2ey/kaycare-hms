using KayCare.Core.DTOs.Referrals;

namespace KayCare.Core.Interfaces;

public interface IReferralService
{
    Task<IReadOnlyList<ReferralSummaryResponse>> GetAllAsync(string? status, CancellationToken ct);
    Task<IReadOnlyList<ReferralSummaryResponse>> GetForPatientAsync(Guid patientId, CancellationToken ct);
    Task<IReadOnlyList<ReferralSummaryResponse>> GetIncomingAsync(CancellationToken ct);
    Task<ReferralDetailResponse>                 GetByIdAsync(Guid id, CancellationToken ct);
    Task<ReferralDetailResponse>                 CreateAsync(CreateReferralRequest req, CancellationToken ct);
    Task<ReferralDetailResponse>                 UpdateAsync(Guid id, UpdateReferralRequest req, CancellationToken ct);
    Task<ReferralDetailResponse>                 SendAsync(Guid id, CancellationToken ct);
    Task<ReferralDetailResponse>                 RespondAsync(Guid id, RespondToReferralRequest req, CancellationToken ct);
    Task<ReferralDetailResponse>                 CancelAsync(Guid id, CancellationToken ct);
}
