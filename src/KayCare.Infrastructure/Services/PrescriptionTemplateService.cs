using KayCare.Core.DTOs.PrescriptionTemplates;
using KayCare.Core.Entities;
using KayCare.Core.Exceptions;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KayCare.Infrastructure.Services;

public class PrescriptionTemplateService : IPrescriptionTemplateService
{
    private readonly AppDbContext        _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext      _tenantContext;

    public PrescriptionTemplateService(AppDbContext db, ICurrentUserService currentUser, ITenantContext tenantContext)
    {
        _db            = db;
        _currentUser   = currentUser;
        _tenantContext = tenantContext;
    }

    public async Task<PrescriptionTemplateDetailResponse> CreateAsync(CreatePrescriptionTemplateRequest req, CancellationToken ct = default)
    {
        if (!req.Items.Any())
            throw new AppException("A template must contain at least one medication.", 400);

        var template = new PrescriptionTemplate
        {
            CreatedByUserId = _currentUser.UserId,
            Name            = req.Name.Trim(),
            Description     = req.Description?.Trim(),
            IsShared        = req.IsShared
        };

        _db.PrescriptionTemplates.Add(template);
        await _db.SaveChangesAsync(ct);

        var items = req.Items.Select(i => new PrescriptionTemplateItem
        {
            TemplateId            = template.TemplateId,
            TenantId              = _tenantContext.TenantId,
            MedicationName        = i.MedicationName.Trim(),
            GenericName           = i.GenericName?.Trim(),
            Strength              = i.Strength.Trim(),
            DosageForm            = i.DosageForm,
            Frequency             = i.Frequency,
            DurationDays          = i.DurationDays,
            Quantity              = i.Quantity,
            Refills               = i.Refills,
            Instructions          = i.Instructions?.Trim(),
            IsControlledSubstance = i.IsControlledSubstance
        }).ToList();

        _db.PrescriptionTemplateItems.AddRange(items);
        await _db.SaveChangesAsync(ct);

        return await LoadDetailAsync(template.TemplateId, ct);
    }

    public async Task<PrescriptionTemplateDetailResponse> UpdateAsync(Guid templateId, CreatePrescriptionTemplateRequest req, CancellationToken ct = default)
    {
        var template = await _db.PrescriptionTemplates
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.TemplateId == templateId, ct)
            ?? throw new NotFoundException(nameof(PrescriptionTemplate), templateId);

        if (template.CreatedByUserId != _currentUser.UserId)
            throw new AppException("You can only edit your own templates.", 403);

        if (!req.Items.Any())
            throw new AppException("A template must contain at least one medication.", 400);

        template.Name        = req.Name.Trim();
        template.Description = req.Description?.Trim();
        template.IsShared    = req.IsShared;

        _db.PrescriptionTemplateItems.RemoveRange(template.Items);

        var newItems = req.Items.Select(i => new PrescriptionTemplateItem
        {
            TemplateId            = templateId,
            TenantId              = _tenantContext.TenantId,
            MedicationName        = i.MedicationName.Trim(),
            GenericName           = i.GenericName?.Trim(),
            Strength              = i.Strength.Trim(),
            DosageForm            = i.DosageForm,
            Frequency             = i.Frequency,
            DurationDays          = i.DurationDays,
            Quantity              = i.Quantity,
            Refills               = i.Refills,
            Instructions          = i.Instructions?.Trim(),
            IsControlledSubstance = i.IsControlledSubstance
        }).ToList();

        _db.PrescriptionTemplateItems.AddRange(newItems);
        await _db.SaveChangesAsync(ct);

        return await LoadDetailAsync(templateId, ct);
    }

    public async Task DeleteAsync(Guid templateId, CancellationToken ct = default)
    {
        var template = await _db.PrescriptionTemplates
            .FirstOrDefaultAsync(t => t.TemplateId == templateId, ct)
            ?? throw new NotFoundException(nameof(PrescriptionTemplate), templateId);

        if (template.CreatedByUserId != _currentUser.UserId)
            throw new AppException("You can only delete your own templates.", 403);

        _db.PrescriptionTemplates.Remove(template);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<PrescriptionTemplateDetailResponse> GetByIdAsync(Guid templateId, CancellationToken ct = default)
        => await LoadDetailAsync(templateId, ct);

    public async Task<IReadOnlyList<PrescriptionTemplateResponse>> GetMyTemplatesAsync(CancellationToken ct = default)
    {
        var rows = await _db.PrescriptionTemplates
            .Include(t => t.CreatedBy)
            .Include(t => t.Items)
            .AsNoTracking()
            .Where(t => t.CreatedByUserId == _currentUser.UserId)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

        return rows.Select(MapToSummary).ToList();
    }

    public async Task<IReadOnlyList<PrescriptionTemplateResponse>> GetSharedTemplatesAsync(CancellationToken ct = default)
    {
        var rows = await _db.PrescriptionTemplates
            .Include(t => t.CreatedBy)
            .Include(t => t.Items)
            .AsNoTracking()
            .Where(t => t.IsShared)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

        return rows.Select(MapToSummary).ToList();
    }

    private async Task<PrescriptionTemplateDetailResponse> LoadDetailAsync(Guid templateId, CancellationToken ct)
    {
        var t = await _db.PrescriptionTemplates
            .Include(t => t.CreatedBy)
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.TemplateId == templateId, ct)
            ?? throw new NotFoundException(nameof(PrescriptionTemplate), templateId);

        return MapToDetail(t);
    }

    private static PrescriptionTemplateResponse MapToSummary(PrescriptionTemplate t) => new()
    {
        TemplateId    = t.TemplateId,
        Name          = t.Name,
        Description   = t.Description,
        IsShared      = t.IsShared,
        CreatedByName = $"{t.CreatedBy.FirstName} {t.CreatedBy.LastName}".Trim(),
        ItemCount     = t.Items.Count,
        CreatedAt     = t.CreatedAt
    };

    private static PrescriptionTemplateDetailResponse MapToDetail(PrescriptionTemplate t) => new()
    {
        TemplateId    = t.TemplateId,
        Name          = t.Name,
        Description   = t.Description,
        IsShared      = t.IsShared,
        CreatedByName = $"{t.CreatedBy.FirstName} {t.CreatedBy.LastName}".Trim(),
        ItemCount     = t.Items.Count,
        CreatedAt     = t.CreatedAt,
        Items         = t.Items.Select(i => new PrescriptionTemplateItemResponse
        {
            TemplateItemId        = i.TemplateItemId,
            MedicationName        = i.MedicationName,
            GenericName           = i.GenericName,
            Strength              = i.Strength,
            DosageForm            = i.DosageForm,
            Frequency             = i.Frequency,
            DurationDays          = i.DurationDays,
            Quantity              = i.Quantity,
            Refills               = i.Refills,
            Instructions          = i.Instructions,
            IsControlledSubstance = i.IsControlledSubstance
        }).ToList()
    };
}
