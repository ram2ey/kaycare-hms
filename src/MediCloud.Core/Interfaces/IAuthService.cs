using MediCloud.Core.DTOs.Auth;

namespace MediCloud.Core.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
}
