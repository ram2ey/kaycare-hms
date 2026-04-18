namespace KayCare.Core.Entities;

public class DispenseEvent
{
    public Guid      DispenseEventId   { get; set; }
    public Guid      TenantId          { get; set; }
    public Guid      PrescriptionId    { get; set; }
    public Guid      DispensedByUserId { get; set; }
    public DateTime  DispensedAt       { get; set; }
    public string?   Notes             { get; set; }

    public Prescription Prescription { get; set; } = null!;
    public User         DispensedBy  { get; set; } = null!;
    public ICollection<DispenseEventItem> Items { get; set; } = [];
}
