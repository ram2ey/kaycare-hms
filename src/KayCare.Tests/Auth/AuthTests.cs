using System.Net;
using KayCare.Tests.Infrastructure;

namespace KayCare.Tests.Auth;

/// <summary>
/// Integration tests for POST /api/auth/login.
/// Covers: successful login, wrong credentials, account lockout, and tenant resolution.
/// </summary>
[Collection("Integration")]
public class AuthTests : IClassFixture<MediCloudWebAppFactory>
{
    private readonly MediCloudWebAppFactory _factory;

    public AuthTests(MediCloudWebAppFactory factory) => _factory = factory;

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithValidCredentials_Returns200AndToken()
    {
        var client = _factory.CreateAnonymousClientForTenant(_factory.TenantA);

        var resp = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email    = _factory.TenantA.AdminEmail,
            Password = MediCloudWebAppFactory.TestPassword,
        });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var body = await resp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var token = body.GetProperty("token").GetString();
        Assert.False(string.IsNullOrEmpty(token));
        Assert.Equal(_factory.TenantA.AdminEmail, body.GetProperty("email").GetString());
        Assert.Equal("Admin", body.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Login_WithDoctorCredentials_ReturnsCorrectRole()
    {
        var client = _factory.CreateAnonymousClientForTenant(_factory.TenantA);

        var resp = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email    = _factory.TenantA.DoctorEmail,
            Password = MediCloudWebAppFactory.TestPassword,
        });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var body = await resp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal("Doctor", body.GetProperty("role").GetString());
    }

    // ── Wrong credentials ─────────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = _factory.CreateAnonymousClientForTenant(_factory.TenantA);

        var resp = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email    = _factory.TenantA.AdminEmail,
            Password = "WrongPassword!",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_Returns401()
    {
        var client = _factory.CreateAnonymousClientForTenant(_factory.TenantA);

        var resp = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email    = "nobody@nowhere.com",
            Password = MediCloudWebAppFactory.TestPassword,
        });

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ── Tenant resolution ─────────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithUnknownTenantCode_Returns404()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Code", "tenant-that-does-not-exist");

        var resp = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email    = _factory.TenantA.AdminEmail,
            Password = MediCloudWebAppFactory.TestPassword,
        });

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_ButWrongTenant_Returns401()
    {
        // TenantA's admin credentials used against TenantB — should not authenticate
        var client = _factory.CreateAnonymousClientForTenant(_factory.TenantB);

        var resp = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email    = _factory.TenantA.AdminEmail,
            Password = MediCloudWebAppFactory.TestPassword,
        });

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ── Account lockout ───────────────────────────────────────────────────────

    [Fact]
    public async Task Login_FiveWrongPasswords_LocksAccount_Returns423()
    {
        // Use a throwaway user so we don't lock the shared test admin
        var email  = await _factory.CreateThrowawayUserAsync(_factory.TenantA);
        var client = _factory.CreateAnonymousClientForTenant(_factory.TenantA);

        // 5 failed attempts
        for (int i = 0; i < 5; i++)
        {
            var r = await client.PostAsJsonAsync("/api/auth/login", new
            {
                Email    = email,
                Password = "WrongPassword!",
            });
            Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
        }

        // 6th attempt — account must now be locked
        var locked = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email    = email,
            Password = "WrongPassword!",
        });

        Assert.Equal(HttpStatusCode.Locked, locked.StatusCode); // 423
    }

    [Fact]
    public async Task Login_AfterLockout_CorrectPassword_StillReturns423()
    {
        // Verify that the correct password doesn't bypass a locked account
        var email  = await _factory.CreateThrowawayUserAsync(_factory.TenantA);
        var client = _factory.CreateAnonymousClientForTenant(_factory.TenantA);

        for (int i = 0; i < 5; i++)
        {
            await client.PostAsJsonAsync("/api/auth/login", new
            {
                Email    = email,
                Password = "WrongPassword!",
            });
        }

        var resp = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email    = email,
            Password = MediCloudWebAppFactory.TestPassword, // correct password
        });

        Assert.Equal(HttpStatusCode.Locked, resp.StatusCode);
    }
}
