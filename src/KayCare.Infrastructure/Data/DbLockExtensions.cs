using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace KayCare.Infrastructure.Data;

public static class DbLockExtensions
{
    public static async Task AcquireAdvisoryLockAsync(this AppDbContext db, Guid tenantId, string sequenceName, CancellationToken ct)
    {
        using var md5 = MD5.Create();
        var rawKey = tenantId.ToString() + sequenceName;
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(rawKey));
        long lockKey = BitConverter.ToInt64(hash, 0);

        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock({0})", new object[] { lockKey }, ct);
    }
}
