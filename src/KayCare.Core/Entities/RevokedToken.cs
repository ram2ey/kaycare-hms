namespace KayCare.Core.Entities;

public class RevokedToken
{
    public Guid     RevokedTokenId { get; set; }
    public Guid     Jti            { get; set; }
    public DateTime ExpiresAt      { get; set; }
    public DateTime RevokedAt      { get; set; }
}
