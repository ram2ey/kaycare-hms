using KayCare.API.Auth;
using KayCare.Core.Constants;
using KayCare.Core.DTOs.Auth;
using KayCare.Core.Interfaces;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KayCare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUser;
    private readonly ITokenRevocationService _tokenRevocation;
    private readonly IAntiforgery _antiforgery;
    private readonly IWebHostEnvironment _env;

    public AuthController(IAuthService authService, ICurrentUserService currentUser,
        ITokenRevocationService tokenRevocation, IAntiforgery antiforgery, IWebHostEnvironment env)
    {
        _authService     = authService;
        _currentUser     = currentUser;
        _tokenRevocation = tokenRevocation;
        _antiforgery     = antiforgery;
        _env             = env;
    }

    /// <summary>
    /// Authenticates a user. The JWT is delivered via an httpOnly cookie, never in the response
    /// body. Requires X-Tenant-Code header or a recognised subdomain.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ProducesResponseType(typeof(LoginResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(423)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var response = await _authService.LoginAsync(request, ct);

        Response.Cookies.Append(AuthCookieNames.Token, response.Token, AuthCookiePolicy.Build(_env, response.ExpiresAt));
        response.CsrfToken = _antiforgery.GetAndStoreTokens(HttpContext).RequestToken!;

        return Ok(response);
    }

    /// <summary>
    /// Returns the current user, or 401. Called on every SPA boot — the httpOnly cookie can't be
    /// read by JS, so this is how "am I logged in" is determined, and it (re)primes the CSRF
    /// token. Must stay AllowAnonymous at the route level (checked inside) so a
    /// must-change-password user's boot check doesn't get rejected by auth middleware first.
    /// </summary>
    [HttpGet("me")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(MeResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        if (User.Identity?.IsAuthenticated != true) return Unauthorized();

        var me = await _authService.GetCurrentUserAsync(_currentUser.UserId, ct);
        if (me is null) return Unauthorized();

        me.CsrfToken = _antiforgery.GetAndStoreTokens(HttpContext).RequestToken!;
        return Ok(me);
    }

    /// <summary>
    /// Clears the auth cookie and revokes the current token server-side (if any), so a copy of
    /// the cookie captured before logout can't keep authenticating. Safe to call without an
    /// active session.
    /// </summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        if (_currentUser.IsAuthenticated && _currentUser.Jti.HasValue && _currentUser.TokenExpiresAt.HasValue)
        {
            await _tokenRevocation.RevokeAsync(_currentUser.Jti.Value, _currentUser.TokenExpiresAt.Value, ct);
        }

        Response.Cookies.Append(AuthCookieNames.Token, string.Empty, AuthCookiePolicy.Build(_env, DateTimeOffset.UnixEpoch));
        return NoContent();
    }

    /// <summary>
    /// Change the current user's password. Clears the MustChangePassword flag and rotates the
    /// auth cookie so the new claim takes effect on the very next request — no client-side hack
    /// or hard reload needed.
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var (token, expiresAt) = await _authService.ChangePasswordAsync(_currentUser.UserId, request, ct);
        Response.Cookies.Append(AuthCookieNames.Token, token, AuthCookiePolicy.Build(_env, expiresAt));
        return NoContent();
    }
}
