namespace KayCare.Core.DTOs.Auth;

public class MeResponse
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string TenantCode { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; }
    public string CsrfToken { get; set; } = string.Empty;
}
