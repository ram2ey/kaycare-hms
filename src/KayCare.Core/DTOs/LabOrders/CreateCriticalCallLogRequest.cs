namespace KayCare.Core.DTOs.LabOrders;

public class CreateCriticalCallLogRequest
{
    public string RecipientName { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
