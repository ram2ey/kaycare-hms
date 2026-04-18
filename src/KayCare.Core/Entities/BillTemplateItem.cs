namespace KayCare.Core.Entities;

public class BillTemplateItem
{
    public Guid     BillTemplateItemId { get; set; }
    public Guid     TenantId           { get; set; }
    public Guid     BillTemplateId     { get; set; }
    public string   Description        { get; set; } = string.Empty;
    public string?  Category           { get; set; }
    public int      Quantity           { get; set; } = 1;
    public decimal  UnitPrice          { get; set; }
    public decimal  TotalPrice         { get; set; }  // computed: Quantity * UnitPrice

    public BillTemplate BillTemplate { get; set; } = null!;
}
