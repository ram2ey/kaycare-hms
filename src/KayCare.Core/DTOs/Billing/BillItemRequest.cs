using System.ComponentModel.DataAnnotations;

namespace KayCare.Core.DTOs.Billing;

public class BillItemRequest
{
    [Required]
    public string   Description { get; set; } = string.Empty;

    public string?  Category    { get; set; }

    [Range(1, int.MaxValue)]
    public int      Quantity    { get; set; } = 1;

    [Range(0, double.MaxValue)]
    public decimal  UnitPrice   { get; set; }
}
