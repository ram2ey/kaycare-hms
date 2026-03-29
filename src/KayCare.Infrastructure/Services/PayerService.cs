using KayCare.Core.DTOs.Billing;
using KayCare.Core.Entities;
using KayCare.Core.Exceptions;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KayCare.Infrastructure.Services;

public class PayerService : IPayerService
{
    private readonly AppDbContext _db;

    public PayerService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<PayerResponse>> GetAllAsync(bool activeOnly, CancellationToken ct = default)
    {
        var query = _db.Payers.AsNoTracking();
        if (activeOnly) query = query.Where(p => p.IsActive);

        return await query
            .OrderBy(p => p.Type)
            .ThenBy(p => p.Name)
            .Select(p => ToResponse(p))
            .ToListAsync(ct);
    }

    public async Task<PayerResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var payer = await _db.Payers.AsNoTracking()
            .FirstOrDefaultAsync(p => p.PayerId == id, ct);
        return payer == null ? null : ToResponse(payer);
    }

    public async Task<PayerResponse> CreateAsync(SavePayerRequest request, CancellationToken ct = default)
    {
        var payer = new Payer
        {
            Name         = request.Name.Trim(),
            Type         = request.Type,
            ContactPhone = request.ContactPhone?.Trim(),
            ContactEmail = request.ContactEmail?.Trim().ToLowerInvariant(),
            Notes        = request.Notes?.Trim(),
            IsActive     = request.IsActive,
        };

        _db.Payers.Add(payer);
        await _db.SaveChangesAsync(ct);
        return ToResponse(payer);
    }

    public async Task<PayerResponse> UpdateAsync(Guid id, SavePayerRequest request, CancellationToken ct = default)
    {
        var payer = await _db.Payers.FirstOrDefaultAsync(p => p.PayerId == id, ct)
            ?? throw new NotFoundException("Payer", id);

        payer.Name         = request.Name.Trim();
        payer.Type         = request.Type;
        payer.ContactPhone = request.ContactPhone?.Trim();
        payer.ContactEmail = request.ContactEmail?.Trim().ToLowerInvariant();
        payer.Notes        = request.Notes?.Trim();
        payer.IsActive     = request.IsActive;

        await _db.SaveChangesAsync(ct);
        return ToResponse(payer);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var payer = await _db.Payers.FirstOrDefaultAsync(p => p.PayerId == id, ct)
            ?? throw new NotFoundException("Payer", id);

        _db.Payers.Remove(payer);
        await _db.SaveChangesAsync(ct);
    }

    private static PayerResponse ToResponse(Payer p) => new()
    {
        PayerId      = p.PayerId,
        Name         = p.Name,
        Type         = p.Type,
        ContactPhone = p.ContactPhone,
        ContactEmail = p.ContactEmail,
        Notes        = p.Notes,
        IsActive     = p.IsActive,
        CreatedAt    = p.CreatedAt,
        UpdatedAt    = p.UpdatedAt,
    };
}
