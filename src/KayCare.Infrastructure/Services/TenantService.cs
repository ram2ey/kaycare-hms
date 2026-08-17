using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using KayCare.Core.Constants;
using KayCare.Core.DTOs.Tenants;
using KayCare.Core.Entities;
using KayCare.Core.Exceptions;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;

namespace KayCare.Infrastructure.Services;

public class TenantService(AppDbContext db, IAuditService audit, ILogger<TenantService> logger) : ITenantService
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

    public async Task<CreateTenantResponse> CreateAsync(CreateTenantRequest req, CancellationToken ct = default)
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

        var tempPassword = GenerateSecureTempPassword();
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

        await audit.LogAsync(AuditActions.TenantCreate, nameof(Tenant), tenantId, null,
            details: $"TenantCode={code}; TenantName={tenant.TenantName}", ct: ct);
        logger.LogInformation("Tenant {TenantId} ({TenantCode}) created with admin user {AdminEmail}", tenantId, code, adminUser.Email);

        var response = ToResponse(tenant, 1);
        return new CreateTenantResponse
        {
            TenantId              = response.TenantId,
            TenantCode            = response.TenantCode,
            TenantName            = response.TenantName,
            Subdomain             = response.Subdomain,
            SubscriptionPlan      = response.SubscriptionPlan,
            IsActive              = response.IsActive,
            MaxUsers              = response.MaxUsers,
            StorageQuotaGB        = response.StorageQuotaGB,
            UserCount             = response.UserCount,
            IsAiEnabled           = response.IsAiEnabled,
            AiMonthlyQuota        = response.AiMonthlyQuota,
            AiRequestsThisMonth   = response.AiRequestsThisMonth,
            AllowedAiTiers        = response.AllowedAiTiers,
            HasCustomOpenRouterKey = response.HasCustomOpenRouterKey,
            CreatedAt             = response.CreatedAt,
            TemporaryPassword     = tempPassword,
        };
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
        // Only overwrite the stored key when a new, non-blank value is submitted — the client
        // never has the real key to send back (see ToResponse), so a blank field always means
        // "leave the existing key untouched," not "clear it."
        if (!string.IsNullOrWhiteSpace(req.CustomOpenRouterKey))
        {
            tenant.CustomOpenRouterKey = req.CustomOpenRouterKey;
        }
        tenant.UpdatedAt            = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        await audit.LogAsync(AuditActions.TenantUpdate, nameof(Tenant), id, null,
            details: $"TenantName={tenant.TenantName}; Plan={tenant.SubscriptionPlan}", ct: ct);
        logger.LogInformation("Tenant {TenantId} updated", id);

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

        await audit.LogAsync(AuditActions.TenantSetActive, nameof(Tenant), id, null,
            details: $"IsActive={isActive}", ct: ct);
        if (isActive)
            logger.LogInformation("Tenant {TenantId} activated", id);
        else
            logger.LogWarning("Tenant {TenantId} deactivated", id);

        var count = await db.Users.CountAsync(u => u.TenantId == id, ct);
        return ToResponse(tenant, count);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var tenant = await db.Tenants.FindAsync([id], ct)
            ?? throw new NotFoundException("Tenant", id);

        db.Tenants.Remove(tenant);
        await db.SaveChangesAsync(ct);

        await audit.LogAsync(AuditActions.TenantDelete, nameof(Tenant), id, null, ct: ct);
        logger.LogWarning("Tenant {TenantId} deleted", id);
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
        HasCustomOpenRouterKey = !string.IsNullOrEmpty(t.CustomOpenRouterKey),
        CreatedAt            = t.CreatedAt,
    };

    private static string GenerateSecureTempPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%^&*";
        var bytes = RandomNumberGenerator.GetBytes(16);
        var sb = new StringBuilder(bytes.Length);
        foreach (var b in bytes)
        {
            sb.Append(chars[b % chars.Length]);
        }
        return sb.ToString();
    }
}
