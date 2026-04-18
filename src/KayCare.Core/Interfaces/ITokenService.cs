using KayCare.Core.Entities;

namespace KayCare.Core.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user, string roleName);
}
