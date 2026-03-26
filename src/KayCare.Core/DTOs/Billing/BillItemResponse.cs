namespace KayCare.Core.DTOs.Billing;

public class BillItemResponse
{
    public Guid    ItemId      { get; set; }
    public string  Description { get; set; } = string.Empty;
    public string? Category    { get; set; }
    public int     Quantity    { get; set; }
    public decimal UnitPrice   { get; set; }
    public decimal TotalPrice  { get; set; }
}
