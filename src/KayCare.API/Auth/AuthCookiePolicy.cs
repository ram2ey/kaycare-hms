namespace KayCare.API.Auth;

public static class AuthCookiePolicy
{
    public static CookieOptions Build(IWebHostEnvironment env, DateTimeOffset expires) => new()
    {
        HttpOnly = true,
        Secure   = !env.IsDevelopment(),
        SameSite = env.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None,
        Path     = "/",
        Expires  = expires,
        // No Domain — host-only cookie. Must match exactly between write and clear or the
        // browser treats it as a different cookie and the clear silently no-ops.
    };
}
