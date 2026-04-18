using KayCare.Core.DTOs.Billing;
using KayCare.Core.Entities;
using KayCare.Core.Exceptions;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KayCare.Infrastructure.Services;

public class ServiceCatalogService : IServiceCatalogService
{
    private readonly AppDbContext _db;

    public ServiceCatalogService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ServiceCatalogItemResponse>> GetAllAsync(bool activeOnly, CancellationToken ct)
    {
        var query = _db.ServiceCatalogItems.AsNoTracking();
        if (activeOnly) query = query.Where(s => s.IsActive);

        return await query
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Name)
            .Select(s => ToResponse(s))
            .ToListAsync(ct);
    }

    public async Task<ServiceCatalogItemResponse?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var item = await _db.ServiceCatalogItems.AsNoTracking()
            .FirstOrDefaultAsync(s => s.ServiceCatalogItemId == id, ct);
        return item == null ? null : ToResponse(item);
    }

    public async Task<ServiceCatalogItemResponse> CreateAsync(SaveServiceCatalogItemRequest request, CancellationToken ct)
    {
        var item = new ServiceCatalogItem
        {
            Name        = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Category    = request.Category,
            UnitPrice   = request.UnitPrice,
            IsActive    = request.IsActive,
        };

        _db.ServiceCatalogItems.Add(item);
        await _db.SaveChangesAsync(ct);
        return ToResponse(item);
    }

    public async Task<ServiceCatalogItemResponse> UpdateAsync(Guid id, SaveServiceCatalogItemRequest request, CancellationToken ct)
    {
        var item = await _db.ServiceCatalogItems
            .FirstOrDefaultAsync(s => s.ServiceCatalogItemId == id, ct)
            ?? throw new NotFoundException("ServiceCatalogItem", id);

        item.Name        = request.Name.Trim();
        item.Description = request.Description?.Trim();
        item.Category    = request.Category;
        item.UnitPrice   = request.UnitPrice;
        item.IsActive    = request.IsActive;

        await _db.SaveChangesAsync(ct);
        return ToResponse(item);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var item = await _db.ServiceCatalogItems
            .FirstOrDefaultAsync(s => s.ServiceCatalogItemId == id, ct)
            ?? throw new NotFoundException("ServiceCatalogItem", id);

        _db.ServiceCatalogItems.Remove(item);
        await _db.SaveChangesAsync(ct);
    }

    private static ServiceCatalogItemResponse ToResponse(ServiceCatalogItem s) => new()
    {
        ServiceCatalogItemId = s.ServiceCatalogItemId,
        Name                 = s.Name,
        Description          = s.Description,
        Category             = s.Category,
        UnitPrice            = s.UnitPrice,
        IsActive             = s.IsActive,
        CreatedAt            = s.CreatedAt,
        UpdatedAt            = s.UpdatedAt,
    };
}
