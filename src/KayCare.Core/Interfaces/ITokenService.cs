using KayCare.Core.Entities;

namespace KayCare.Core.Interfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) GenerateToken(User user, string roleName);
}
