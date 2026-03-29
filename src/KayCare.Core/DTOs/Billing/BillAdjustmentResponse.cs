namespace KayCare.Core.DTOs.Billing;

public class BillAdjustmentResponse
{
    public Guid     BillAdjustmentId { get; set; }
    public decimal  Amount           { get; set; }
    public string   Reason           { get; set; } = string.Empty;
    public string   AdjustedByName   { get; set; } = string.Empty;
    public DateTime AdjustedAt       { get; set; }
}
