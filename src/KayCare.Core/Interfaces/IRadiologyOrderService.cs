using KayCare.Core.DTOs.Radiology;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KayCare.Core.Interfaces;

public interface IRadiologyOrderService
{
    Task<IReadOnlyList<ImagingProcedureResponse>>   GetProcedureCatalogAsync(CancellationToken ct);
    Task<RadiologyOrderDetailResponse>              PlaceOrderAsync(CreateRadiologyOrderRequest req, CancellationToken ct);
    Task<IReadOnlyList<RadiologyOrderResponse>>     GetByPatientAsync(Guid patientId, CancellationToken ct);
    Task<IReadOnlyList<RadiologyOrderResponse>>     GetWorklistAsync(DateOnly date, string? status, CancellationToken ct);
    Task<RadiologyOrderDetailResponse?>             GetByIdAsync(Guid radiologyOrderId, CancellationToken ct);
    Task<RadiologyOrderItemResponse>                MarkAcquiredAsync(Guid itemId, string? pacsUrl, CancellationToken ct);
    Task<RadiologyOrderItemResponse>                EnterReportAsync(Guid itemId, RadiologyReportRequest req, CancellationToken ct);
    Task<RadiologyOrderItemResponse>                SignItemAsync(Guid itemId, CancellationToken ct);
    Task<RadiologyStatsResponse>                    GetStatsAsync(CancellationToken ct);
}

public interface IRadiologyReportService
{
    Task<byte[]?> GenerateReportAsync(Guid radiologyOrderId, CancellationToken ct);
}
