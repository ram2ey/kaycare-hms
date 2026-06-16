using KayCare.Core.DTOs.Billing;

namespace KayCare.Core.Interfaces;

public interface IPayerTariffService
{
    /// <summary>All tariffs for a given payer.</summary>
    Task<List<PayerTariffResponse>> GetByPayerAsync(Guid payerId, CancellationToken ct = default);

    /// <summary>All payer tariffs configured for a specific service catalog item.</summary>
    Task<List<PayerTariffResponse>> GetByServiceItemAsync(Guid serviceItemId, CancellationToken ct = default);

    /// <summary>All active tariffs for the tenant (optionally filtered by active only).</summary>
    Task<List<PayerTariffResponse>> GetAllAsync(bool activeOnly = true, CancellationToken ct = default);

    /// <summary>
    /// Create or update a tariff. Idempotent on (TenantId, PayerId, ServiceCatalogItemId).
    /// If a record already exists it is updated; otherwise a new one is created.
    /// </summary>
    Task<PayerTariffResponse> UpsertAsync(SavePayerTariffRequest request, CancellationToken ct = default);

    Task<PayerTariffResponse> UpdateAsync(Guid id, SavePayerTariffRequest request, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
