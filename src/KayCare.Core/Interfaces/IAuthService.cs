using KayCare.Core.DTOs.Auth;

namespace KayCare.Core.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<(string Token, DateTime ExpiresAt)> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default);
    Task<MeResponse?> GetCurrentUserAsync(Guid userId, CancellationToken ct = default);
}
