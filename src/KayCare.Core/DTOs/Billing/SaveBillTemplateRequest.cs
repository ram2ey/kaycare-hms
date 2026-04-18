namespace KayCare.Core.DTOs.Billing;

public class BillTemplateItemRequest
{
    public string  Description { get; set; } = string.Empty;
    public string? Category    { get; set; }
    public int     Quantity    { get; set; } = 1;
    public decimal UnitPrice   { get; set; }
}

public class SaveBillTemplateRequest
{
    public string  Name        { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category    { get; set; }
    public bool    IsActive    { get; set; } = true;
    public List<BillTemplateItemRequest> Items { get; set; } = [];
}
