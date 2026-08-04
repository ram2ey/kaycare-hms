using Microsoft.EntityFrameworkCore;
using KayCare.Core.DTOs.Tenants;
using KayCare.Core.Entities;
using KayCare.Core.Exceptions;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;

namespace KayCare.Infrastructure.Services;

public class TenantService(AppDbContext db) : ITenantService
{
    public async Task<List<TenantResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var tenants = await db.Tenants.AsNoTracking().ToListAsync(ct);
        var userCounts = await db.Users
            .AsNoTracking()
            .GroupBy(u => u.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count, ct);

        return tenants.Select(t => ToResponse(t, userCounts.GetValueOrDefault(t.TenantId, 0))).ToList();
    }

    public async Task<TenantResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var tenant = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.TenantId == id, ct)
            ?? throw new NotFoundException("Tenant", id);

        var userCount = await db.Users.AsNoTracking().CountAsync(u => u.TenantId == id, ct);
        return ToResponse(tenant, userCount);
    }

    public async Task<TenantResponse> CreateAsync(CreateTenantRequest req, CancellationToken ct = default)
    {
        var code = req.TenantCode.Trim().ToUpperInvariant();
        if (await db.Tenants.AnyAsync(t => t.TenantCode == code, ct))
            throw new ConflictException($"Tenant code '{code}' is already registered.");

        if (await db.Users.AnyAsync(u => u.Email == req.AdminEmail.Trim().ToLowerInvariant(), ct))
            throw new ConflictException($"Email '{req.AdminEmail.Trim()}' is already in use.");

        var now      = DateTime.UtcNow;
        var tenantId = Guid.NewGuid();

        var tenant = new Tenant
        {
            TenantId             = tenantId,
            TenantCode           = code,
            TenantName           = req.TenantName.Trim(),
            Subdomain            = code,
            SubscriptionPlan     = req.SubscriptionPlan,
            IsActive             = true,
            MaxUsers             = req.MaxUsers,
            StorageQuotaGB       = req.StorageQuotaGB,
            IsAiEnabled          = req.IsAiEnabled,
            AiMonthlyQuota       = req.AiMonthlyQuota,
            AiRequestsThisMonth  = 0,
            AiQuotaResetDate     = now,
            AllowedAiTiers       = req.AllowedAiTiers,
            CustomOpenRouterKey = req.CustomOpenRouterKey,
            CreatedAt            = now,
            UpdatedAt            = now,
        };

        var tempPassword = $"Welcome@{DateTime.UtcNow.Year}!";
        var hash         = BCrypt.Net.BCrypt.HashPassword(tempPassword, 12);

        var adminUser = new
        {
            UserId        = Guid.NewGuid(),
            RoleId        = 2, // Admin
            Email         = req.AdminEmail.Trim().ToLowerInvariant(),
            PasswordHash  = hash,
            FirstName     = req.AdminFirstName.Trim(),
            LastName      = req.AdminLastName.Trim(),
        };

        db.Tenants.Add(tenant);
        await db.SaveChangesAsync(ct);

        await db.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO Users
              (UserId, TenantId, RoleId, Email, PasswordHash,
               FirstName, LastName, IsActive, MustChangePassword,
               FailedLoginCount, CreatedAt, UpdatedAt)
            VALUES
              ({adminUser.UserId}, {tenantId}, {adminUser.RoleId}, {adminUser.Email},
               {adminUser.PasswordHash}, {adminUser.FirstName}, {adminUser.LastName},
               {1}, {1}, {0}, {now}, {now})", ct);

        return ToResponse(tenant, 1);
    }

    public async Task<TenantResponse> UpdateAsync(Guid id, UpdateTenantRequest req, CancellationToken ct = default)
    {
        var tenant = await db.Tenants.FindAsync([id], ct)
            ?? throw new NotFoundException("Tenant", id);

        tenant.TenantName           = req.TenantName.Trim();
        tenant.SubscriptionPlan     = req.SubscriptionPlan;
        tenant.MaxUsers             = req.MaxUsers;
        tenant.StorageQuotaGB       = req.StorageQuotaGB;
        tenant.IsAiEnabled          = req.IsAiEnabled;
        tenant.AiMonthlyQuota       = req.AiMonthlyQuota;
        tenant.AllowedAiTiers       = req.AllowedAiTiers;
        tenant.CustomOpenRouterKey = req.CustomOpenRouterKey;
        tenant.UpdatedAt            = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        var count = await db.Users.CountAsync(u => u.TenantId == id, ct);
        return ToResponse(tenant, count);
    }

    public async Task<TenantResponse> SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        var tenant = await db.Tenants.FindAsync([id], ct)
            ?? throw new NotFoundException("Tenant", id);

        tenant.IsActive  = isActive;
        tenant.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var count = await db.Users.CountAsync(u => u.TenantId == id, ct);
        return ToResponse(tenant, count);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var tenant = await db.Tenants.FindAsync([id], ct)
            ?? throw new NotFoundException("Tenant", id);

        db.Tenants.Remove(tenant);
        await db.SaveChangesAsync(ct);
    }

    private static TenantResponse ToResponse(Tenant t, int userCount) => new()
    {
        TenantId             = t.TenantId,
        TenantCode           = t.TenantCode,
        TenantName           = t.TenantName,
        Subdomain            = t.Subdomain,
        SubscriptionPlan     = t.SubscriptionPlan,
        IsActive             = t.IsActive,
        MaxUsers             = t.MaxUsers,
        StorageQuotaGB       = t.StorageQuotaGB,
        UserCount            = userCount,
        IsAiEnabled          = t.IsAiEnabled,
        AiMonthlyQuota       = t.AiMonthlyQuota,
        AiRequestsThisMonth  = t.AiRequestsThisMonth,
        AllowedAiTiers       = t.AllowedAiTiers,
        CustomOpenRouterKey = t.CustomOpenRouterKey,
        CreatedAt            = t.CreatedAt,
    };
}
