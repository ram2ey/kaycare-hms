namespace KayCare.Core.DTOs.Billing;

public class AddPaymentRequest
{
    public decimal  Amount        { get; set; }
    public string   PaymentMethod { get; set; } = string.Empty;
    public string?  Reference     { get; set; }
    public string?  Notes         { get; set; }
}
