using KayCare.Core.Constants;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using KayCare.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KayCare.Tests.Infrastructure;

/// <summary>
/// Shared test server for all integration tests.
/// Overrides the connection string → KayCareTestDb on PostgreSQL.
/// Disables the MLLP TCP listener (port 2575 not needed in tests).
/// Applies EF migrations and seeds two isolated test tenants on first start.
/// </summary>
public class MediCloudWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>Known JWT signing key used by all tests.</summary>
    public const string TestJwtKey = "test-signing-key-kaycare-integration-!!";

    /// <summary>Password used for every seeded test user.</summary>
    public const string TestPassword = "TestPass123!";

    /// <summary>Seeded tenant A — "Hospital A".</summary>
    public TestTenant TenantA { get; private set; } = null!;

    /// <summary>Seeded tenant B — "Hospital B".</summary>
    public TestTenant TenantB { get; private set; } = null!;

    // ── WebApplicationFactory overrides ───────────────────────────────────────

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Override configuration values — InMemoryCollection is added last so it wins
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    Environment.GetEnvironmentVariable("TEST_DB_CONNECTION")
                    ?? "Host=localhost;Database=KayCareTestDb;Username=postgres;Password=postgres;",
                ["Jwt:Key"]         = TestJwtKey,
                ["Jwt:Issuer"]      = "KayCare",
                ["Jwt:Audience"]    = "KayCare",
                ["Jwt:ExpiryHours"] = "8",
                ["Hl7:WebhookApiKey"]    = "test-hl7-webhook-key-kaycare-integration",
                ["Hl7:MllpSharedSecret"] = "test-hl7-mllp-secret-kaycare-integration",
                // Azurite dev connection string — blob operations not tested here
                ["BlobStorage:ConnectionString"] =
                    "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;" +
                    "AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;" +
                    "BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the MLLP background service — it tries to bind TCP port 2575
            var mllp = services.SingleOrDefault(
                d => d.ImplementationType == typeof(MllpListenerService));
            if (mllp is not null)
                services.Remove(mllp);
        });
    }

    // ── IAsyncLifetime ────────────────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        using var scope     = Services.CreateScope();
        var db              = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tenantCtx       = scope.ServiceProvider.GetRequiredService<ITenantContext>();

        // Apply any pending migrations (creates MediCloudTestDb if it doesn't exist)
        await db.Database.MigrateAsync();

        // Seed two isolated tenants + users for this test run
        (TenantA, TenantB) = await TestSeeder.SeedAsync(db, tenantCtx);
    }

    public new Task DisposeAsync() => Task.CompletedTask; // Leave test DB for inspection

    // ── HTTP client helpers ───────────────────────────────────────────────────

    /// <summary>
    /// Creates an HttpClient that is logged in as the given user and pre-configured
    /// with the X-Tenant-Code header for the given tenant.
    /// </summary>
    /// <remarks>
    /// The JWT is delivered via an httpOnly Set-Cookie, not the response body (frontend security
    /// finding #1 — never exposing the token to JS). Tests extract it from the cookie and resend
    /// it as an explicit Authorization: Bearer header rather than relying on HttpClient's cookie
    /// jar, exercising the same header-based fallback path Swagger/tooling use — that path is
    /// deliberately kept alive and is CSRF-exempt by design (see CsrfProtectionMiddleware).
    /// </remarks>
    public async Task<HttpClient> CreateAuthenticatedClientAsync(TestTenant tenant, string email)
    {
        var client = CreateHttpsClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Code", tenant.TenantCode);

        var resp = await client.PostAsJsonAsync("/api/auth/login",
            new { Email = email, Password = TestPassword });
        resp.EnsureSuccessStatusCode();

        var token = ExtractAuthCookieValue(resp)
            ?? throw new InvalidOperationException("Login response did not set the auth cookie.");

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    /// <summary>Pulls the raw JWT value out of the Set-Cookie header written by AuthController.</summary>
    internal static string? ExtractAuthCookieValue(HttpResponseMessage resp)
    {
        if (!resp.Headers.TryGetValues("Set-Cookie", out var cookies)) return null;

        var prefix = AuthCookieNames.Token + "=";
        foreach (var cookie in cookies)
        {
            if (!cookie.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;

            var value     = cookie[prefix.Length..];
            var semicolon = value.IndexOf(';');
            return semicolon >= 0 ? value[..semicolon] : value;
        }

        return null;
    }

    public Task<HttpClient> CreateAdminClientAsync(TestTenant tenant) =>
        CreateAuthenticatedClientAsync(tenant, tenant.AdminEmail);

    public Task<HttpClient> CreateDoctorClientAsync(TestTenant tenant) =>
        CreateAuthenticatedClientAsync(tenant, tenant.DoctorEmail);

    /// <summary>Returns a client with the tenant header set but no auth token.</summary>
    public HttpClient CreateAnonymousClientForTenant(TestTenant tenant)
    {
        var client = CreateHttpsClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Code", tenant.TenantCode);
        return client;
    }

    /// <summary>
    /// Base <see cref="WebApplicationFactory{TEntryPoint}.CreateClient()"/> talks over
    /// plain http://localhost, but production always serves over HTTPS (Render). The antiforgery
    /// system enforces this at runtime (AntiforgeryOptions.Cookie.SecurePolicy = Always outside
    /// Development) and throws rather than degrading if a request isn't HTTPS — so tests need the
    /// in-memory TestServer to see requests as HTTPS too, or every antiforgery-touching endpoint
    /// (including login) fails with a 500 that has nothing to do with the code under test.
    /// </summary>
    private HttpClient CreateHttpsClient() =>
        CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

    /// <summary>
    /// Inserts a fresh throwaway user directly into the DB for tests that need
    /// a disposable account (e.g. lockout tests).
    /// </summary>
    public async Task<string> CreateThrowawayUserAsync(TestTenant tenant, int roleId = 2)
    {
        using var scope    = Services.CreateScope();
        var db             = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tenantCtx      = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        return await TestSeeder.CreateThrowawayUserAsync(db, tenantCtx, tenant.TenantId, roleId);
    }
}
