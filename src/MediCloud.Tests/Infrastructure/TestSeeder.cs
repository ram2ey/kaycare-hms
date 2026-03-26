using MediCloud.Core.Entities;
using MediCloud.Core.Interfaces;
using MediCloud.Infrastructure.Data;

namespace MediCloud.Tests.Infrastructure;

public static class TestSeeder
{
    /// <summary>
    /// Creates two isolated tenants (A and B) with an Admin and Doctor user each.
    /// A unique suffix is appended so repeated test runs never collide.
    /// Password for every seeded user: <see cref="MediCloudWebAppFactory.TestPassword"/>.
    /// </summary>
    public static async Task<(TestTenant A, TestTenant B)> SeedAsync(
        AppDbContext db, ITenantContext tenantCtx)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        // Low work-factor so seeding is fast; real cost factor used by AuthService at runtime
        var hash = BCrypt.Net.BCrypt.HashPassword(
            MediCloudWebAppFactory.TestPassword, workFactor: 4);

        // ── Create tenants ─────────────────────────────────────────────────────
        var now = DateTime.UtcNow;

        var tenantA = new Tenant
        {
            TenantId   = Guid.NewGuid(),
            TenantCode = $"testa{suffix}",
            TenantName = "Test Hospital A",
            Subdomain  = $"testa{suffix}",
            IsActive   = true,
            CreatedAt  = now,
            UpdatedAt  = now,
        };

        var tenantB = new Tenant
        {
            TenantId   = Guid.NewGuid(),
            TenantCode = $"testb{suffix}",
            TenantName = "Test Hospital B",
            Subdomain  = $"testb{suffix}",
            IsActive   = true,
            CreatedAt  = now,
            UpdatedAt  = now,
        };

        db.Tenants.AddRange(tenantA, tenantB);
        await db.SaveChangesAsync(); // Tenant is not TenantEntity — no auto-inject

        // ── TenantA users ──────────────────────────────────────────────────────
        tenantCtx.TenantId = tenantA.TenantId; // SaveChangesAsync reads this for TenantEntity

        var adminA  = MakeUser($"admin-a-{suffix}@test.local",  hash, roleId: 2); // Admin
        var doctorA = MakeUser($"doctor-a-{suffix}@test.local", hash, roleId: 3); // Doctor
        db.Users.AddRange(adminA, doctorA);
        await db.SaveChangesAsync();

        // ── TenantB users ──────────────────────────────────────────────────────
        tenantCtx.TenantId = tenantB.TenantId;

        var adminB  = MakeUser($"admin-b-{suffix}@test.local",  hash, roleId: 2);
        var doctorB = MakeUser($"doctor-b-{suffix}@test.local", hash, roleId: 3);
        db.Users.AddRange(adminB, doctorB);
        await db.SaveChangesAsync();

        return (
            new TestTenant(tenantA.TenantId, tenantA.TenantCode,
                adminA.UserId, adminA.Email, doctorA.UserId, doctorA.Email),
            new TestTenant(tenantB.TenantId, tenantB.TenantCode,
                adminB.UserId, adminB.Email, doctorB.UserId, doctorB.Email)
        );
    }

    /// <summary>Creates a single throwaway user for a specific tenant (e.g. lockout tests).</summary>
    public static async Task<string> CreateThrowawayUserAsync(
        AppDbContext db, ITenantContext tenantCtx, Guid tenantId, int roleId = 2)
    {
        tenantCtx.TenantId = tenantId;
        var email = $"throwaway-{Guid.NewGuid():N}@test.local";
        var user  = MakeUser(email,
            BCrypt.Net.BCrypt.HashPassword(MediCloudWebAppFactory.TestPassword, workFactor: 4),
            roleId);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return email;
    }

    private static User MakeUser(string email, string hash, int roleId) => new()
    {
        // UserId left as Guid.Empty → DB generates NEWSEQUENTIALID()
        RoleId             = roleId,
        Email              = email,
        PasswordHash       = hash,
        FirstName          = "Test",
        LastName           = "User",
        IsActive           = true,
        MustChangePassword = false,
        FailedLoginCount   = 0,
    };
}
