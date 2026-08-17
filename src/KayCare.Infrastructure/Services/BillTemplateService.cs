using KayCare.Core.Constants;
using KayCare.Core.DTOs.Billing;
using KayCare.Core.Entities;
using KayCare.Core.Exceptions;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KayCare.Infrastructure.Services;

public class BillTemplateService : IBillTemplateService
{
    private readonly AppDbContext   _db;
    private readonly ITenantContext _tenantContext;
    private readonly IAuditService  _audit;
    private readonly ILogger<BillTemplateService> _logger;

    public BillTemplateService(AppDbContext db, ITenantContext tenantContext, IAuditService audit, ILogger<BillTemplateService> logger)
    {
        _db            = db;
        _tenantContext = tenantContext;
        _audit         = audit;
        _logger        = logger;
    }

    public async Task<List<BillTemplateResponse>> GetAllAsync(
        bool includeInactive = false, string? category = null, CancellationToken ct = default)
    {
        var query = _db.BillTemplates.Include(t => t.Items).AsQueryable();

        if (!includeInactive)
            query = query.Where(t => t.IsActive);

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(t => t.Category == category);

        var templates = await query.OrderBy(t => t.Category).ThenBy(t => t.Name).ToListAsync(ct);
        return templates.Select(Map).ToList();
    }

    public async Task<BillTemplateResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Map(await FindAsync(id, ct));

    public async Task<BillTemplateResponse> CreateAsync(SaveBillTemplateRequest request, CancellationToken ct = default)
    {
        var exists = await _db.BillTemplates
            .AnyAsync(t => t.Name == request.Name.Trim(), ct);
        if (exists)
            throw new ConflictException($"A template named '{request.Name.Trim()}' already exists.");

        var template = new BillTemplate
        {
            BillTemplateId = Guid.NewGuid(),
            Name           = request.Name.Trim(),
            Description    = request.Description?.Trim(),
            Category       = request.Category?.Trim(),
            IsActive       = request.IsActive,
            Items          = MapItems(request.Items, Guid.Empty), // TenantId set below in SaveChanges
        };

        // Set TenantId on items manually (they don't inherit TenantEntity)
        foreach (var item in template.Items)
            item.TenantId = _tenantContext.TenantId;

        _db.BillTemplates.Add(template);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditActions.BillTemplateCreate, nameof(BillTemplate), template.BillTemplateId, null,
            details: $"Name={template.Name}", ct: ct);
        _logger.LogInformation("BillTemplate {BillTemplateId} ({Name}) created", template.BillTemplateId, template.Name);

        return Map(template);
    }

    public async Task<BillTemplateResponse> UpdateAsync(Guid id, SaveBillTemplateRequest request, CancellationToken ct = default)
    {
        var template = await FindAsync(id, ct);

        var nameConflict = await _db.BillTemplates
            .AnyAsync(t => t.Name == request.Name.Trim() && t.BillTemplateId != id, ct);
        if (nameConflict)
            throw new ConflictException($"A template named '{request.Name.Trim()}' already exists.");

        template.Name        = request.Name.Trim();
        template.Description = request.Description?.Trim();
        template.Category    = request.Category?.Trim();
        template.IsActive    = request.IsActive;
        template.UpdatedAt   = DateTime.UtcNow;

        // Full replace of items
        _db.BillTemplateItems.RemoveRange(template.Items);
        template.Items = MapItems(request.Items, _tenantContext.TenantId);

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditActions.BillTemplateUpdate, nameof(BillTemplate), template.BillTemplateId, null, ct: ct);
        _logger.LogInformation("BillTemplate {BillTemplateId} updated", template.BillTemplateId);

        return Map(template);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var template = await FindAsync(id, ct);
        _db.BillTemplates.Remove(template);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditActions.BillTemplateDelete, nameof(BillTemplate), template.BillTemplateId, null,
            details: $"Name={template.Name}", ct: ct);
        _logger.LogWarning("BillTemplate {BillTemplateId} ({Name}) deleted", template.BillTemplateId, template.Name);
    }

    public async Task<BillTemplateResponse> ToggleActiveAsync(Guid id, CancellationToken ct = default)
    {
        var template = await FindAsync(id, ct);
        template.IsActive  = !template.IsActive;
        template.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditActions.BillTemplateToggleActive, nameof(BillTemplate), template.BillTemplateId, null,
            details: $"IsActive={template.IsActive}", ct: ct);
        if (template.IsActive)
            _logger.LogInformation("BillTemplate {BillTemplateId} activated", template.BillTemplateId);
        else
            _logger.LogWarning("BillTemplate {BillTemplateId} deactivated", template.BillTemplateId);

        return Map(template);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private async Task<BillTemplate> FindAsync(Guid id, CancellationToken ct)
        => await _db.BillTemplates.Include(t => t.Items)
               .FirstOrDefaultAsync(t => t.BillTemplateId == id, ct)
           ?? throw new NotFoundException("BillTemplate", id);

    private ICollection<BillTemplateItem> MapItems(List<BillTemplateItemRequest> items, Guid tenantId)
        => items.Select(i => new BillTemplateItem
        {
            BillTemplateItemId = Guid.NewGuid(),
            TenantId           = tenantId,
            Description        = i.Description.Trim(),
            Category           = i.Category?.Trim(),
            Quantity           = i.Quantity < 1 ? 1 : i.Quantity,
            UnitPrice          = i.UnitPrice,
        }).ToList();

    private static BillTemplateResponse Map(BillTemplate t) => new()
    {
        BillTemplateId = t.BillTemplateId,
        Name           = t.Name,
        Description    = t.Description,
        Category       = t.Category,
        IsActive       = t.IsActive,
        TotalValue     = t.Items.Sum(i => i.Quantity * i.UnitPrice),
        Items          = t.Items.Select(i => new BillTemplateItemResponse
        {
            BillTemplateItemId = i.BillTemplateItemId,
            Description        = i.Description,
            Category           = i.Category,
            Quantity           = i.Quantity,
            UnitPrice          = i.UnitPrice,
            TotalPrice         = i.TotalPrice,
        }).ToList(),
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt,
    };
}
