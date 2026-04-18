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
/// Overrides the connection string → MediCloudTestDb on local SQL Server Express.
/// Disables the MLLP TCP listener (port 2575 not needed in tests).
/// Applies EF migrations and seeds two isolated test tenants on first start.
/// </summary>
public class MediCloudWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>Known JWT signing key used by all tests.</summary>
    public const string TestJwtKey = "test-signing-key-medicloud-integration-!!";

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
                    ?? @"Server=.\SQLEXPRESS;Database=KayCareTestDb;Integrated Security=True;TrustServerCertificate=True;",
                ["Jwt:Key"]         = TestJwtKey,
                ["Jwt:Issuer"]      = "KayCare",
                ["Jwt:Audience"]    = "KayCare",
                ["Jwt:ExpiryHours"] = "8",
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
    public async Task<HttpClient> CreateAuthenticatedClientAsync(TestTenant tenant, string email)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Code", tenant.TenantCode);

        var resp = await client.PostAsJsonAsync("/api/auth/login",
            new { Email = email, Password = TestPassword });
        resp.EnsureSuccessStatusCode();

        var body  = await resp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var token = body.GetProperty("token").GetString()!;

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    public Task<HttpClient> CreateAdminClientAsync(TestTenant tenant) =>
        CreateAuthenticatedClientAsync(tenant, tenant.AdminEmail);

    public Task<HttpClient> CreateDoctorClientAsync(TestTenant tenant) =>
        CreateAuthenticatedClientAsync(tenant, tenant.DoctorEmail);

    /// <summary>Returns a client with the tenant header set but no auth token.</summary>
    public HttpClient CreateAnonymousClientForTenant(TestTenant tenant)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Code", tenant.TenantCode);
        return client;
    }

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
