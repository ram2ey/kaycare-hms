using MediCloud.Core.Entities;

namespace MediCloud.Core.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user, string roleName);
}
