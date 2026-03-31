using KayCare.Core.DTOs.Billing;

namespace KayCare.Core.Interfaces;

public interface IInsuranceClaimService
{
    Task<InsuranceClaimResponse>       CreateAsync(CreateClaimRequest request, CancellationToken ct = default);
    Task<List<InsuranceClaimResponse>> GetAllAsync(string? status, Guid? payerId, Guid? patientId, CancellationToken ct = default);
    Task<InsuranceClaimResponse?>      GetByIdAsync(Guid claimId, CancellationToken ct = default);
    Task<InsuranceClaimResponse>       SubmitAsync(Guid claimId, CancellationToken ct = default);
    Task<InsuranceClaimResponse>       ApproveAsync(Guid claimId, ApproveClaimRequest request, CancellationToken ct = default);
    Task<InsuranceClaimResponse>       RejectAsync(Guid claimId, RejectClaimRequest request, CancellationToken ct = default);
    Task<InsuranceClaimResponse>       CancelAsync(Guid claimId, CancellationToken ct = default);
    Task<byte[]?>                      GenerateClaimPdfAsync(Guid claimId, CancellationToken ct = default);
}
