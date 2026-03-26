using MediCloud.Core.Interfaces;

namespace MediCloud.Infrastructure.Services;

public class TenantContext : ITenantContext
{
    public Guid TenantId { get; set; }
    public string TenantCode { get; set; } = string.Empty;
}
