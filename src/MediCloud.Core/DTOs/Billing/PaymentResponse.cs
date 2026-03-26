namespace MediCloud.Core.DTOs.Billing;

public class PaymentResponse
{
    public Guid     PaymentId       { get; set; }
    public decimal  Amount          { get; set; }
    public string   PaymentMethod   { get; set; } = string.Empty;
    public string?  Reference       { get; set; }
    public string   ReceivedByName  { get; set; } = string.Empty;
    public DateTime PaymentDate     { get; set; }
    public string?  Notes           { get; set; }
    public DateTime CreatedAt       { get; set; }
}
