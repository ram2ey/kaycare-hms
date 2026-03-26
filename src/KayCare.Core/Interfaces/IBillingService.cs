using KayCare.Core.DTOs.Billing;

namespace KayCare.Core.Interfaces;

public interface IBillingService
{
    Task<BillDetailResponse> CreateAsync(CreateBillRequest request, CancellationToken ct = default);
    Task<BillDetailResponse> GetByIdAsync(Guid billId, CancellationToken ct = default);
    Task<IReadOnlyList<BillResponse>> GetPatientBillsAsync(Guid patientId, CancellationToken ct = default);
    Task<IReadOnlyList<BillResponse>> GetOutstandingAsync(CancellationToken ct = default);
    Task<BillDetailResponse> IssueAsync(Guid billId, CancellationToken ct = default);
    Task<BillDetailResponse> AddPaymentAsync(Guid billId, AddPaymentRequest request, CancellationToken ct = default);
    Task<BillDetailResponse> CancelAsync(Guid billId, CancellationToken ct = default);
    Task<BillDetailResponse> VoidAsync(Guid billId, CancellationToken ct = default);
}
