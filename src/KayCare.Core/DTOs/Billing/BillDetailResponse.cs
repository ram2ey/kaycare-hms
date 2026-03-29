namespace KayCare.Core.DTOs.Billing;

public class BillDetailResponse : BillResponse
{
    public Guid?   ConsultationId { get; set; }
    public Guid?   PayerId        { get; set; }
    public string? PayerName      { get; set; }
    public string? DiscountReason { get; set; }
    public string  CreatedByName  { get; set; } = string.Empty;
    public string? Notes          { get; set; }
    public DateTime UpdatedAt     { get; set; }
    public List<BillItemResponse> Items    { get; set; } = [];
    public List<PaymentResponse>  Payments { get; set; } = [];
}
