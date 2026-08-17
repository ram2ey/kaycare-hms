using System.ComponentModel.DataAnnotations;

namespace KayCare.Core.DTOs.Billing;

public class CreateBillRequest
{
    public Guid    PatientId      { get; set; }
    public Guid?   ConsultationId { get; set; }
    public Guid?   PayerId        { get; set; }

    [Range(0, double.MaxValue)]
    public decimal DiscountAmount { get; set; }

    public string? DiscountReason { get; set; }
    public string? Notes          { get; set; }
    public List<BillItemRequest> Items { get; set; } = [];
}
