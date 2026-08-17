namespace KayCare.Core.Interfaces;

public interface ITokenRevocationService
{
    /// <summary>Revokes a token by its jti, effective until the token's own expiry.</summary>
    Task RevokeAsync(Guid jti, DateTime expiresAt, CancellationToken ct = default);

    /// <summary>True if the given jti has been revoked and hasn't naturally expired yet.</summary>
    Task<bool> IsRevokedAsync(Guid jti, CancellationToken ct = default);
}
