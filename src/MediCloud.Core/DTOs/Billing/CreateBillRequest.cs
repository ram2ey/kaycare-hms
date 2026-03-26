namespace MediCloud.Core.DTOs.Billing;

public class CreateBillRequest
{
    public Guid   PatientId      { get; set; }
    public Guid?  ConsultationId { get; set; }
    public string? Notes         { get; set; }
    public List<BillItemRequest> Items { get; set; } = [];
}
