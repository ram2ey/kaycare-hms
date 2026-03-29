using KayCare.Core.DTOs.Billing;

namespace KayCare.Core.Interfaces;

public interface IBillingReportsService
{
    Task<ArAgingReport> GetArAgingAsync(CancellationToken ct = default);
    Task<RevenueDashboardResponse> GetRevenueDashboardAsync(CancellationToken ct = default);
}
