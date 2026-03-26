namespace KayCare.Core.DTOs.Billing;

public class BillItemRequest
{
    public string   Description { get; set; } = string.Empty;
    public string?  Category    { get; set; }
    public int      Quantity    { get; set; } = 1;
    public decimal  UnitPrice   { get; set; }
}
