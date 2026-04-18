using KayCare.Core.Interfaces;

namespace KayCare.Infrastructure.Services;

public class TenantContext : ITenantContext
{
    public Guid TenantId { get; set; }
    public string TenantCode { get; set; } = string.Empty;
}
