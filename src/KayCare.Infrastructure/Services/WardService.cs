using KayCare.Core.Constants;
using KayCare.Core.DTOs.Inpatient;
using KayCare.Core.Entities;
using KayCare.Core.Exceptions;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KayCare.Infrastructure.Services;

public class WardService : IWardService
{
    private readonly AppDbContext _db;

    public WardService(AppDbContext db) => _db = db;

    public async Task<List<WardResponse>> GetAllAsync(bool? activeOnly = true, CancellationToken ct = default)
    {
        var query = _db.Wards.Include(w => w.Beds).AsNoTracking();
        if (activeOnly == true) query = query.Where(w => w.IsActive);
        return await query.OrderBy(w => w.WardType).ThenBy(w => w.Name)
            .Select(w => ToResponse(w)).ToListAsync(ct);
    }

    public async Task<WardResponse?> GetByIdAsync(Guid wardId, CancellationToken ct = default)
    {
        var ward = await _db.Wards.Include(w => w.Beds).AsNoTracking()
            .FirstOrDefaultAsync(w => w.WardId == wardId, ct);
        return ward == null ? null : ToResponse(ward);
    }

    public async Task<WardResponse> CreateAsync(SaveWardRequest request, CancellationToken ct = default)
    {
        if (!WardType.All.Contains(request.WardType))
            throw new AppException($"Invalid ward type '{request.WardType}'.", 400);

        var ward = new Ward
        {
            Name        = request.Name.Trim(),
            WardType    = request.WardType,
            Description = request.Description?.Trim(),
            DailyRate   = request.DailyRate,
            IsActive    = request.IsActive,
        };
        _db.Wards.Add(ward);
        await _db.SaveChangesAsync(ct);
        return ToResponse(ward);
    }

    public async Task<WardResponse> UpdateAsync(Guid wardId, SaveWardRequest request, CancellationToken ct = default)
    {
        if (!WardType.All.Contains(request.WardType))
            throw new AppException($"Invalid ward type '{request.WardType}'.", 400);

        var ward = await _db.Wards.Include(w => w.Beds)
            .FirstOrDefaultAsync(w => w.WardId == wardId, ct)
            ?? throw new NotFoundException("Ward", wardId);

        ward.Name        = request.Name.Trim();
        ward.WardType    = request.WardType;
        ward.Description = request.Description?.Trim();
        ward.DailyRate   = request.DailyRate;
        ward.IsActive    = request.IsActive;
        await _db.SaveChangesAsync(ct);
        return ToResponse(ward);
    }

    public async Task<WardResponse> DeactivateAsync(Guid wardId, CancellationToken ct = default)
    {
        var ward = await _db.Wards.Include(w => w.Beds)
            .FirstOrDefaultAsync(w => w.WardId == wardId, ct)
            ?? throw new NotFoundException("Ward", wardId);

        ward.IsActive = false;
        await _db.SaveChangesAsync(ct);
        return ToResponse(ward);
    }

    public async Task<List<BedResponse>> GetBedsAsync(Guid wardId, CancellationToken ct = default)
    {
        _ = await _db.Wards.AsNoTracking().FirstOrDefaultAsync(w => w.WardId == wardId, ct)
            ?? throw new NotFoundException("Ward", wardId);

        return await _db.Beds.AsNoTracking()
            .Include(b => b.Ward)
            .Where(b => b.WardId == wardId)
            .OrderBy(b => b.BedNumber)
            .Select(b => ToBedResponse(b))
            .ToListAsync(ct);
    }

    public async Task<BedResponse> AddBedAsync(Guid wardId, SaveBedRequest request, CancellationToken ct = default)
    {
        var ward = await _db.Wards.FirstOrDefaultAsync(w => w.WardId == wardId, ct)
            ?? throw new NotFoundException("Ward", wardId);

        var bed = new Bed
        {
            WardId    = wardId,
            BedNumber = request.BedNumber.Trim(),
            Status    = BedStatus.Available,
            Notes     = request.Notes?.Trim(),
        };
        _db.Beds.Add(bed);
        await _db.SaveChangesAsync(ct);

        bed.Ward = ward;
        return ToBedResponse(bed);
    }

    public async Task<BedResponse> UpdateBedStatusAsync(Guid bedId, UpdateBedStatusRequest request, CancellationToken ct = default)
    {
        if (!BedStatus.All.Contains(request.Status))
            throw new AppException($"Invalid bed status '{request.Status}'.", 400);

        var bed = await _db.Beds.Include(b => b.Ward)
            .FirstOrDefaultAsync(b => b.BedId == bedId, ct)
            ?? throw new NotFoundException("Bed", bedId);

        bed.Status = request.Status;
        if (request.Notes != null) bed.Notes = request.Notes.Trim();
        await _db.SaveChangesAsync(ct);
        return ToBedResponse(bed);
    }

    public async Task<BedResponse> UpdateBedAsync(Guid bedId, SaveBedRequest request, CancellationToken ct = default)
    {
        var bed = await _db.Beds.Include(b => b.Ward)
            .FirstOrDefaultAsync(b => b.BedId == bedId, ct)
            ?? throw new NotFoundException("Bed", bedId);

        bed.BedNumber = request.BedNumber.Trim();
        bed.Notes     = request.Notes?.Trim();
        await _db.SaveChangesAsync(ct);
        return ToBedResponse(bed);
    }

    public async Task DeleteBedAsync(Guid bedId, CancellationToken ct = default)
    {
        var bed = await _db.Beds.FirstOrDefaultAsync(b => b.BedId == bedId, ct)
            ?? throw new NotFoundException("Bed", bedId);

        if (bed.Status == BedStatus.Occupied)
            throw new AppException("Cannot delete an occupied bed.", 400);

        _db.Beds.Remove(bed);
        await _db.SaveChangesAsync(ct);
    }

    private static WardResponse ToResponse(Ward w) => new()
    {
        WardId          = w.WardId,
        Name            = w.Name,
        WardType        = w.WardType,
        Description     = w.Description,
        DailyRate       = w.DailyRate,
        IsActive        = w.IsActive,
        TotalBeds       = w.Beds.Count,
        AvailableBeds   = w.Beds.Count(b => b.Status == BedStatus.Available),
        OccupiedBeds    = w.Beds.Count(b => b.Status == BedStatus.Occupied),
        MaintenanceBeds = w.Beds.Count(b => b.Status == BedStatus.Maintenance),
        CreatedAt       = w.CreatedAt,
        UpdatedAt       = w.UpdatedAt,
    };

    private static BedResponse ToBedResponse(Bed b) => new()
    {
        BedId     = b.BedId,
        WardId    = b.WardId,
        WardName  = b.Ward?.Name ?? string.Empty,
        BedNumber = b.BedNumber,
        Status    = b.Status,
        Notes     = b.Notes,
        CreatedAt = b.CreatedAt,
        UpdatedAt = b.UpdatedAt,
    };
}
