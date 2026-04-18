namespace KayCare.Core.DTOs.Billing;

public class BillTemplateItemResponse
{
    public Guid    BillTemplateItemId { get; set; }
    public string  Description        { get; set; } = string.Empty;
    public string? Category           { get; set; }
    public int     Quantity           { get; set; }
    public decimal UnitPrice          { get; set; }
    public decimal TotalPrice         { get; set; }
}

public class BillTemplateResponse
{
    public Guid     BillTemplateId { get; set; }
    public string   Name           { get; set; } = string.Empty;
    public string?  Description    { get; set; }
    public string?  Category       { get; set; }
    public bool     IsActive       { get; set; }
    public decimal  TotalValue     { get; set; }
    public List<BillTemplateItemResponse> Items { get; set; } = [];
    public DateTime CreatedAt      { get; set; }
    public DateTime UpdatedAt      { get; set; }
}
