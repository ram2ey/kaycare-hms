namespace KayCare.Core.Interfaces;

public interface ICurrentUserService
{
    Guid   UserId        { get; }
    Guid   TenantId      { get; }
    string Email         { get; }
    string Role          { get; }
    bool   IsAuthenticated { get; }
    Guid?     Jti             { get; }
    DateTime? TokenExpiresAt  { get; }
}
