using KayCare.Core.Constants;
using KayCare.Core.DTOs.Auth;
using KayCare.Core.Entities;
using KayCare.Core.Exceptions;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KayCare.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUser;
    private readonly ITokenRevocationService _tokenRevocation;
    private readonly IAuditService _audit;
    private readonly ILogger<AuthService> _logger;

    // Computed once at process startup. Verified against on the not-found/inactive-user path below
    // so that path pays the same ~250-300ms BCrypt cost as a real user's wrong-password path —
    // otherwise a client could distinguish "no such account" from "wrong password" by timing alone.
    private static readonly string DummyPasswordHash =
        BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString(), workFactor: 12);

    public AuthService(AppDbContext db, ITokenService tokenService, ITenantContext tenantContext,
        ICurrentUserService currentUser, ITokenRevocationService tokenRevocation,
        IAuditService audit, ILogger<AuthService> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _tokenRevocation = tokenRevocation;
        _audit = audit;
        _logger = logger;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        // IgnoreQueryFilters: we filter by TenantId explicitly so the global filter isn't double-applied
        var user = await _db.Users
            .Include(u => u.Role)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.TenantId == _tenantContext.TenantId && u.Email == email, ct);

        if (user is null || !user.IsActive)
        {
            BCrypt.Net.BCrypt.Verify(request.Password, DummyPasswordHash);
            _logger.LogWarning("Login failed for {Email}: no such active user", email);
            throw new UnauthorizedException();
        }

        // Account lockout check (HIPAA: 5 failed attempts = 30 min lock)
        if (user.LockedUntil.HasValue && user.LockedUntil > DateTime.UtcNow)
        {
            _logger.LogWarning("Login blocked for {Email}: account locked until {LockedUntil}", email, user.LockedUntil.Value);
            throw new AccountLockedException(user.LockedUntil.Value);
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            user.FailedLoginCount++;
            if (user.FailedLoginCount >= 5)
                user.LockedUntil = DateTime.UtcNow.AddMinutes(30);

            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            _logger.LogWarning("Login failed for {Email}: invalid password (failed attempt {FailedLoginCount})", email, user.FailedLoginCount);
            throw new UnauthorizedException();
        }

        // Success — reset lockout state
        user.FailedLoginCount = 0;
        user.LockedUntil = null;
        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var (token, expiresAt) = _tokenService.GenerateToken(user, user.Role.RoleName);

        await _audit.LogAsync(AuditActions.UserLogin, nameof(User), user.UserId, null,
            details: $"Email={user.Email}", ct: ct);
        _logger.LogInformation("User {UserId} ({Email}) logged in successfully", user.UserId, user.Email);

        return new LoginResponse
        {
            Token              = token,
            ExpiresAt          = expiresAt,
            UserId             = user.UserId.ToString(),
            Email              = user.Email,
            FullName           = $"{user.FirstName} {user.LastName}",
            Role               = user.Role.RoleName,
            TenantCode         = _tenantContext.TenantCode,
            MustChangePassword = user.MustChangePassword,
        };
    }

    public async Task<(string Token, DateTime ExpiresAt)> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users
            .Include(u => u.Role)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.UserId == userId && u.TenantId == _tenantContext.TenantId, ct)
            ?? throw new UnauthorizedException("User not found.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            throw new ValidationException("Current password is incorrect.");

        user.PasswordHash       = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);
        user.MustChangePassword = false;
        user.FailedLoginCount   = 0;
        user.LockedUntil        = null;
        user.UpdatedAt          = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditActions.UserPasswordChange, nameof(User), userId, null, ct: ct);
        _logger.LogInformation("User {UserId} changed their password", userId);

        // Revoke the pre-change-password token so a cached/stolen copy of it can't keep
        // authenticating after the password (and its MustChangePassword claim) has moved on.
        if (_currentUser.Jti.HasValue && _currentUser.TokenExpiresAt.HasValue)
        {
            await _tokenRevocation.RevokeAsync(_currentUser.Jti.Value, _currentUser.TokenExpiresAt.Value, ct);
        }

        // Reissue only after the save is confirmed — a failed save must never imply a token was
        // issued for a password change that didn't actually persist. The old token still carries
        // MustChangePassword=true until this one is set as the new cookie by the controller.
        return _tokenService.GenerateToken(user, user.Role.RoleName);
    }

    public async Task<MeResponse?> GetCurrentUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users
            .Include(u => u.Role)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.UserId == userId && u.TenantId == _tenantContext.TenantId, ct);

        if (user is null || !user.IsActive) return null;

        return new MeResponse
        {
            UserId             = user.UserId.ToString(),
            Email              = user.Email,
            FullName           = $"{user.FirstName} {user.LastName}",
            Role               = user.Role.RoleName,
            TenantCode         = _tenantContext.TenantCode,
            MustChangePassword = user.MustChangePassword,
        };
    }
}
