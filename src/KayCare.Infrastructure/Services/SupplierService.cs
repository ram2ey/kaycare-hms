using KayCare.Core.Constants;
using KayCare.Core.DTOs.Pharmacy;
using KayCare.Core.Entities;
using KayCare.Core.Exceptions;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KayCare.Infrastructure.Services;

public class SupplierService : ISupplierService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;
    private readonly ILogger<SupplierService> _logger;

    public SupplierService(AppDbContext db, IAuditService audit, ILogger<SupplierService> logger)
    {
        _db = db;
        _audit = audit;
        _logger = logger;
    }

    public async Task<List<SupplierResponse>> GetAllAsync(bool? activeOnly = null, CancellationToken ct = default)
    {
        var query = _db.Suppliers.AsNoTracking();

        if (activeOnly == true)
            query = query.Where(s => s.IsActive);

        return await query
            .OrderBy(s => s.Name)
            .Select(s => ToResponse(s))
            .ToListAsync(ct);
    }

    public async Task<SupplierResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var s = await _db.Suppliers.AsNoTracking()
            .FirstOrDefaultAsync(s => s.SupplierId == id, ct);
        return s == null ? null : ToResponse(s);
    }

    public async Task<SupplierResponse> CreateAsync(SaveSupplierRequest request, CancellationToken ct = default)
    {
        var supplier = new Supplier
        {
            Name        = request.Name.Trim(),
            ContactName = request.ContactName?.Trim(),
            Phone       = request.Phone?.Trim(),
            Email       = request.Email?.Trim(),
            Address     = request.Address?.Trim(),
            Notes       = request.Notes?.Trim(),
            IsActive    = request.IsActive,
        };

        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditActions.SupplierCreate, nameof(Supplier), supplier.SupplierId, null,
            details: $"Name={supplier.Name}", ct: ct);
        _logger.LogInformation("Supplier {SupplierId} ({Name}) created", supplier.SupplierId, supplier.Name);

        return ToResponse(supplier);
    }

    public async Task<SupplierResponse> UpdateAsync(Guid id, SaveSupplierRequest request, CancellationToken ct = default)
    {
        var supplier = await _db.Suppliers
            .FirstOrDefaultAsync(s => s.SupplierId == id, ct)
            ?? throw new NotFoundException("Supplier", id);

        supplier.Name        = request.Name.Trim();
        supplier.ContactName = request.ContactName?.Trim();
        supplier.Phone       = request.Phone?.Trim();
        supplier.Email       = request.Email?.Trim();
        supplier.Address     = request.Address?.Trim();
        supplier.Notes       = request.Notes?.Trim();
        supplier.IsActive    = request.IsActive;

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditActions.SupplierUpdate, nameof(Supplier), supplier.SupplierId, null, ct: ct);
        _logger.LogInformation("Supplier {SupplierId} updated", supplier.SupplierId);

        return ToResponse(supplier);
    }

    public async Task<SupplierResponse> DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var supplier = await _db.Suppliers
            .FirstOrDefaultAsync(s => s.SupplierId == id, ct)
            ?? throw new NotFoundException("Supplier", id);

        supplier.IsActive = false;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditActions.SupplierDeactivate, nameof(Supplier), supplier.SupplierId, null, ct: ct);
        _logger.LogWarning("Supplier {SupplierId} ({Name}) deactivated", supplier.SupplierId, supplier.Name);

        return ToResponse(supplier);
    }

    private static SupplierResponse ToResponse(Supplier s) => new()
    {
        SupplierId  = s.SupplierId,
        Name        = s.Name,
        ContactName = s.ContactName,
        Phone       = s.Phone,
        Email       = s.Email,
        Address     = s.Address,
        Notes       = s.Notes,
        IsActive    = s.IsActive,
        CreatedAt   = s.CreatedAt,
        UpdatedAt   = s.UpdatedAt,
    };
}
