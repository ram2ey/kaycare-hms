namespace KayCare.Core.Entities;

public class DispenseEventItem
{
    public Guid DispenseEventItemId  { get; set; }
    public Guid TenantId             { get; set; }
    public Guid DispenseEventId      { get; set; }
    public Guid PrescriptionItemId   { get; set; }
    public int  QuantityDispensed    { get; set; }

    public DispenseEvent     Event              { get; set; } = null!;
    public PrescriptionItem  PrescriptionItem   { get; set; } = null!;
}
