using KayCare.Core.DTOs.Billing;
using KayCare.Core.Entities;
using KayCare.Core.Exceptions;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KayCare.Infrastructure.Services;

public class PayerTariffService : IPayerTariffService
{
    private readonly AppDbContext _db;

    public PayerTariffService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<PayerTariffResponse>> GetByPayerAsync(Guid payerId, CancellationToken ct = default)
    {
        return await _db.PayerTariffs
            .AsNoTracking()
            .Include(t => t.Payer)
            .Include(t => t.ServiceCatalogItem)
            .Where(t => t.PayerId == payerId)
            .Select(t => ToResponse(t))
            .ToListAsync(ct);
    }

    public async Task<List<PayerTariffResponse>> GetByServiceItemAsync(Guid serviceItemId, CancellationToken ct = default)
    {
        return await _db.PayerTariffs
            .AsNoTracking()
            .Include(t => t.Payer)
            .Include(t => t.ServiceCatalogItem)
            .Where(t => t.ServiceCatalogItemId == serviceItemId)
            .Select(t => ToResponse(t))
            .ToListAsync(ct);
    }

    public async Task<List<PayerTariffResponse>> GetAllAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        var query = _db.PayerTariffs
            .AsNoTracking()
            .Include(t => t.Payer)
            .Include(t => t.ServiceCatalogItem)
            .AsQueryable();

        if (activeOnly)
        {
            query = query.Where(t => t.IsActive);
        }

        return await query
            .Select(t => ToResponse(t))
            .ToListAsync(ct);
    }

    public async Task<PayerTariffResponse> UpsertAsync(SavePayerTariffRequest request, CancellationToken ct = default)
    {
        var payerExists = await _db.Payers.AnyAsync(p => p.PayerId == request.PayerId, ct);
        if (!payerExists)
        {
            throw new NotFoundException("Payer", request.PayerId);
        }

        var serviceItemExists = await _db.ServiceCatalogItems.AnyAsync(s => s.ServiceCatalogItemId == request.ServiceCatalogItemId, ct);
        if (!serviceItemExists)
        {
            throw new NotFoundException("ServiceCatalogItem", request.ServiceCatalogItemId);
        }

        var tariff = await _db.PayerTariffs
            .Include(t => t.Payer)
            .Include(t => t.ServiceCatalogItem)
            .FirstOrDefaultAsync(t => t.PayerId == request.PayerId && t.ServiceCatalogItemId == request.ServiceCatalogItemId, ct);

        if (tariff == null)
        {
            tariff = new PayerTariff
            {
                PayerId = request.PayerId,
                ServiceCatalogItemId = request.ServiceCatalogItemId,
            };
            ApplyFields(tariff, request);
            _db.PayerTariffs.Add(tariff);
        }
        else
        {
            ApplyFields(tariff, request);
        }

        await _db.SaveChangesAsync(ct);

        var updatedTariff = await _db.PayerTariffs
            .Include(t => t.Payer)
            .Include(t => t.ServiceCatalogItem)
            .FirstAsync(t => t.PayerTariffId == tariff.PayerTariffId, ct);

        return ToResponse(updatedTariff);
    }

    public async Task<PayerTariffResponse> UpdateAsync(Guid id, SavePayerTariffRequest request, CancellationToken ct = default)
    {
        var tariff = await _db.PayerTariffs
            .Include(t => t.Payer)
            .Include(t => t.ServiceCatalogItem)
            .FirstOrDefaultAsync(t => t.PayerTariffId == id, ct)
            ?? throw new NotFoundException("PayerTariff", id);

        ApplyFields(tariff, request);

        await _db.SaveChangesAsync(ct);
        return ToResponse(tariff);
    }

    private static void ApplyFields(PayerTariff tariff, SavePayerTariffRequest request)
    {
        tariff.TariffCode = request.TariffCode?.Trim();
        tariff.TariffPrice = request.TariffPrice;
        tariff.EffectiveDate = request.EffectiveDate;
        tariff.IsActive = request.IsActive;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var tariff = await _db.PayerTariffs
            .FirstOrDefaultAsync(t => t.PayerTariffId == id, ct)
            ?? throw new NotFoundException("PayerTariff", id);

        _db.PayerTariffs.Remove(tariff);
        await _db.SaveChangesAsync(ct);
    }

    private static PayerTariffResponse ToResponse(PayerTariff t) => new()
    {
        PayerTariffId = t.PayerTariffId,
        PayerId = t.PayerId,
        PayerName = t.Payer != null ? t.Payer.Name : string.Empty,
        ServiceCatalogItemId = t.ServiceCatalogItemId,
        ServiceName = t.ServiceCatalogItem != null ? t.ServiceCatalogItem.Name : string.Empty,
        ServiceCategory = t.ServiceCatalogItem != null ? t.ServiceCatalogItem.Category : string.Empty,
        StandardPrice = t.ServiceCatalogItem != null ? t.ServiceCatalogItem.UnitPrice : 0,
        TariffCode = t.TariffCode,
        TariffPrice = t.TariffPrice,
        EffectiveDate = t.EffectiveDate,
        IsActive = t.IsActive,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt
    };
}
