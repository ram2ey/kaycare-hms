using System.ComponentModel.DataAnnotations;

namespace KayCare.Core.DTOs.Billing;

public class ApplyDiscountRequest
{
    [Range(0, double.MaxValue, ErrorMessage = "Discount amount cannot be negative.")]
    public decimal DiscountAmount { get; set; }

    [MaxLength(500)]
    public string? DiscountReason { get; set; }
}
