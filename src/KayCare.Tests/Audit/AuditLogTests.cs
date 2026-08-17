using KayCare.Core.Entities;
using KayCare.Infrastructure.Data;
using KayCare.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KayCare.Tests.Audit;

/// <summary>
/// Verifies the AuditLogs append-only trigger (L3/DB16) actually rejects UPDATE/DELETE at the
/// database level — not just by convention (AuditService never issuing them), since a bug or a
/// direct DB session bypasses that convention entirely.
/// </summary>
[Collection("Integration")]
public class AuditLogTests : IClassFixture<MediCloudWebAppFactory>
{
    private readonly MediCloudWebAppFactory _factory;

    public AuditLogTests(MediCloudWebAppFactory factory) => _factory = factory;

    private static async Task<AuditLog> InsertTestLogAsync(AppDbContext db, Guid tenantId)
    {
        var log = new AuditLog
        {
            TenantId   = tenantId,
            UserId     = Guid.NewGuid(),
            UserEmail  = "trigger-test@example.com",
            Action     = "Test.Action",
            EntityType = "Test",
            EntityId   = Guid.NewGuid(),
            Timestamp  = DateTime.UtcNow,
        };
        db.AuditLogs.Add(log);
        await db.SaveChangesAsync();
        return log;
    }

    [Fact]
    public async Task AuditLogs_RejectsUpdate_AtTheDatabaseLevel()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var log = await InsertTestLogAsync(db, _factory.TenantA.TenantId);

        log.UserEmail = "tampered@example.com";
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Contains("append-only", (ex.InnerException?.Message ?? ex.Message), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AuditLogs_RejectsDelete_AtTheDatabaseLevel()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var log = await InsertTestLogAsync(db, _factory.TenantA.TenantId);

        db.AuditLogs.Remove(log);
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Contains("append-only", (ex.InnerException?.Message ?? ex.Message), StringComparison.OrdinalIgnoreCase);
    }
}
