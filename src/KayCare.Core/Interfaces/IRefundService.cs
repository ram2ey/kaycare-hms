using KayCare.Core.DTOs.Billing;

namespace KayCare.Core.Interfaces;

public interface IRefundService
{
    Task<RefundResponse>       CreateAsync(CreateRefundRequest request, CancellationToken ct = default);
    Task<List<RefundResponse>> GetAllAsync(string? status, Guid? billId, Guid? patientId, CancellationToken ct = default);
    Task<RefundResponse?>      GetByIdAsync(Guid refundId, CancellationToken ct = default);
    Task<RefundResponse>       ProcessAsync(Guid refundId, CancellationToken ct = default);
    Task<RefundResponse>       CancelAsync(Guid refundId, CancellationToken ct = default);
}
