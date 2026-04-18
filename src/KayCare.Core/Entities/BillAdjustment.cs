namespace KayCare.Core.Entities;

public class BillAdjustment
{
    public Guid     BillAdjustmentId  { get; set; }
    public Guid     BillId            { get; set; }
    public Guid     TenantId          { get; set; }
    public decimal  Amount            { get; set; }   // negative = credit, positive = extra charge
    public string   Reason            { get; set; } = string.Empty;
    public Guid     AdjustedByUserId  { get; set; }
    public DateTime AdjustedAt        { get; set; }

    public Bill Bill           { get; set; } = null!;
    public User AdjustedBy     { get; set; } = null!;
}
