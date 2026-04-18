using KayCare.Core.DTOs.Billing;

namespace KayCare.Core.Interfaces;

public interface IServiceCatalogService
{
    Task<List<ServiceCatalogItemResponse>> GetAllAsync(bool activeOnly, CancellationToken ct);
    Task<ServiceCatalogItemResponse?>      GetByIdAsync(Guid id, CancellationToken ct);
    Task<ServiceCatalogItemResponse>       CreateAsync(SaveServiceCatalogItemRequest request, CancellationToken ct);
    Task<ServiceCatalogItemResponse>       UpdateAsync(Guid id, SaveServiceCatalogItemRequest request, CancellationToken ct);
    Task                                   DeleteAsync(Guid id, CancellationToken ct);
}
