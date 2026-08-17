using KayCare.Core.Entities;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KayCare.Infrastructure.Services;

public class TokenRevocationService : ITokenRevocationService
{
    private readonly AppDbContext _db;

    public TokenRevocationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task RevokeAsync(Guid jti, DateTime expiresAt, CancellationToken ct = default)
    {
        // Opportunistic cleanup on every write — bounds table growth without a scheduled job,
        // since a row is only ever meaningful until the token it guards naturally expires.
        var expired = _db.RevokedTokens.Where(t => t.ExpiresAt < DateTime.UtcNow);
        _db.RevokedTokens.RemoveRange(expired);

        _db.RevokedTokens.Add(new RevokedToken
        {
            Jti       = jti,
            ExpiresAt = expiresAt,
            RevokedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> IsRevokedAsync(Guid jti, CancellationToken ct = default)
    {
        return await _db.RevokedTokens.AnyAsync(t => t.Jti == jti && t.ExpiresAt > DateTime.UtcNow, ct);
    }
}
