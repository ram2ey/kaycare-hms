using KayCare.Core.DTOs.Inpatient;

namespace KayCare.Core.Interfaces;

public interface IInpatientBillingService
{
    Task<List<InpatientChargeResponse>>  GetChargesAsync(Guid admissionId, CancellationToken ct = default);
    Task<InpatientBillSummaryResponse>   GetBillSummaryAsync(Guid admissionId, CancellationToken ct = default);
    Task<InpatientChargeResponse>        AddChargeAsync(Guid admissionId, SaveInpatientChargeRequest request, CancellationToken ct = default);
    Task<InpatientChargeResponse>        UpdateChargeAsync(Guid chargeId, SaveInpatientChargeRequest request, CancellationToken ct = default);
    Task                                 RemoveChargeAsync(Guid chargeId, CancellationToken ct = default);
    Task<List<InpatientChargeResponse>>  ApplyAccommodationChargesAsync(Guid admissionId, CancellationToken ct = default);
    Task<Guid>                           GenerateBillAsync(Guid admissionId, CancellationToken ct = default);
}
