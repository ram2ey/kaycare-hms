namespace MediCloud.Core.DTOs.LabOrders;

public class LabOrderDetailResponse : LabOrderResponse
{
    public IReadOnlyList<LabOrderItemResponse> Items { get; set; } = [];
}
