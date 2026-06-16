using System;

namespace KayCare.Core.Entities;

public class CriticalCallLog : TenantEntity
{
    public Guid CriticalCallLogId { get; set; }
    public Guid LabOrderItemId { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public string CalledByName { get; set; } = string.Empty;
    public DateTime CalledAt { get; set; }
    public string Notes { get; set; } = string.Empty;

    public LabOrderItem LabOrderItem { get; set; } = null!;
}
