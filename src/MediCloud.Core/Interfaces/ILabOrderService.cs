using MediCloud.Core.DTOs.LabOrders;

namespace MediCloud.Core.Interfaces;

public interface ILabOrderService
{
    // Test catalog
    Task<IReadOnlyList<LabTestCatalogResponse>> GetTestCatalogAsync(CancellationToken ct);

    // Orders
    Task<LabOrderDetailResponse>               PlaceOrderAsync(CreateLabOrderRequest req, CancellationToken ct);
    Task<IReadOnlyList<LabOrderResponse>>      GetWaitingListAsync(DateOnly date, string? status, string? department, CancellationToken ct);
    Task<IReadOnlyList<LabOrderResponse>>      GetByPatientAsync(Guid patientId, CancellationToken ct);
    Task<LabOrderDetailResponse?>              GetByIdAsync(Guid labOrderId, CancellationToken ct);

    // Item actions
    Task<LabOrderItemResponse> ReceiveSampleAsync(Guid labOrderItemId, CancellationToken ct);
    Task<LabOrderItemResponse> EnterManualResultAsync(Guid labOrderItemId, ManualResultRequest req, CancellationToken ct);
    Task<LabOrderItemResponse> SignItemAsync(Guid labOrderItemId, CancellationToken ct);
}
