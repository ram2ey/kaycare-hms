using System.ComponentModel.DataAnnotations;

namespace KayCare.Core.DTOs.Billing;

public class AddPaymentRequest
{
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
    public decimal  Amount        { get; set; }

    [Required]
    public string   PaymentMethod { get; set; } = string.Empty;

    public string?  Reference     { get; set; }
    public string?  Notes         { get; set; }
}
