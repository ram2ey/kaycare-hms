using KayCare.Core.DTOs.Billing;

namespace KayCare.Core.Interfaces;

public interface IBillTemplateService
{
    Task<List<BillTemplateResponse>> GetAllAsync(bool includeInactive = false, string? category = null, CancellationToken ct = default);
    Task<BillTemplateResponse>       GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<BillTemplateResponse>       CreateAsync(SaveBillTemplateRequest request, CancellationToken ct = default);
    Task<BillTemplateResponse>       UpdateAsync(Guid id, SaveBillTemplateRequest request, CancellationToken ct = default);
    Task                             DeleteAsync(Guid id, CancellationToken ct = default);
    Task<BillTemplateResponse>       ToggleActiveAsync(Guid id, CancellationToken ct = default);
}
