namespace KayCare.Core.DTOs.LabOrders;

public class CreateLabOrderRequest
{
    public Guid   PatientId       { get; set; }
    public Guid?  ConsultationId  { get; set; }
    public Guid?  BillId          { get; set; }

    /// <summary>DIRECT for walk-ins; otherwise referring facility name.</summary>
    public string Organisation { get; set; } = "DIRECT";
    public string? Notes       { get; set; }

    /// <summary>List of LabTestCatalogId values to include in this order.</summary>
    public List<Guid> TestIds { get; set; } = [];
}
