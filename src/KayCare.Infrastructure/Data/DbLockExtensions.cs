using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace KayCare.Infrastructure.Data;

public static class DbLockExtensions
{
    public static async Task AcquireAdvisoryLockAsync(this AppDbContext db, Guid tenantId, string sequenceName, CancellationToken ct)
    {
        long lockKey = ComputeLockKey(tenantId.ToString() + sequenceName);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock({0})", new object[] { lockKey }, ct);
    }

    /// <summary>
    /// Session-level (not transaction-scoped) advisory lock — caller must pair with
    /// <see cref="ReleaseSessionAdvisoryLockAsync"/> in a finally block. Use for operations that
    /// manage their own internal transactions (e.g. EF migrations, which run each migration in its
    /// own transaction), where pg_advisory_xact_lock — which auto-releases on commit — can't safely
    /// wrap the whole operation.
    /// </summary>
    public static async Task AcquireSessionAdvisoryLockAsync(this AppDbContext db, string lockName, CancellationToken ct)
    {
        long lockKey = ComputeLockKey(lockName);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_lock({0})", new object[] { lockKey }, ct);
    }

    public static async Task ReleaseSessionAdvisoryLockAsync(this AppDbContext db, string lockName, CancellationToken ct)
    {
        long lockKey = ComputeLockKey(lockName);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_unlock({0})", new object[] { lockKey }, ct);
    }

    private static long ComputeLockKey(string raw)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return BitConverter.ToInt64(hash, 0);
    }
}
