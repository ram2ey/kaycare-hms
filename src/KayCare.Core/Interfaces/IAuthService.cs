using KayCare.Core.DTOs.Auth;

namespace KayCare.Core.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
}
