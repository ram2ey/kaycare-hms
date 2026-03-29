using KayCare.Core.DTOs.Billing;

namespace KayCare.Core.Interfaces;

public interface IPayerService
{
    Task<List<PayerResponse>> GetAllAsync(bool activeOnly, CancellationToken ct = default);
    Task<PayerResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PayerResponse> CreateAsync(SavePayerRequest request, CancellationToken ct = default);
    Task<PayerResponse> UpdateAsync(Guid id, SavePayerRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
