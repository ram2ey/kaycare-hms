using KayCare.Core.DTOs.Inpatient;

namespace KayCare.Core.Interfaces;

public interface IWardService
{
    Task<List<WardResponse>>  GetAllAsync(bool? activeOnly = true, CancellationToken ct = default);
    Task<WardResponse?>       GetByIdAsync(Guid wardId, CancellationToken ct = default);
    Task<WardResponse>        CreateAsync(SaveWardRequest request, CancellationToken ct = default);
    Task<WardResponse>        UpdateAsync(Guid wardId, SaveWardRequest request, CancellationToken ct = default);
    Task<WardResponse>        DeactivateAsync(Guid wardId, CancellationToken ct = default);
    Task<List<BedResponse>>   GetBedsAsync(Guid wardId, CancellationToken ct = default);
    Task<BedResponse>         AddBedAsync(Guid wardId, SaveBedRequest request, CancellationToken ct = default);
    Task<BedResponse>         UpdateBedStatusAsync(Guid bedId, UpdateBedStatusRequest request, CancellationToken ct = default);
    Task<BedResponse>         UpdateBedAsync(Guid bedId, SaveBedRequest request, CancellationToken ct = default);
    Task                      DeleteBedAsync(Guid bedId, CancellationToken ct = default);
}
