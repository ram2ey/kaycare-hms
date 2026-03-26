namespace MediCloud.Core.Entities;

public class BillItem
{
    public Guid     ItemId      { get; set; }
    public Guid     TenantId    { get; set; }
    public Guid     BillId      { get; set; }
    public string   Description { get; set; } = string.Empty;
    public string?  Category    { get; set; }
    public int      Quantity    { get; set; }
    public decimal  UnitPrice   { get; set; }
    public decimal  TotalPrice  { get; set; }   // computed: Quantity * UnitPrice

    public Bill Bill { get; set; } = null!;
}
