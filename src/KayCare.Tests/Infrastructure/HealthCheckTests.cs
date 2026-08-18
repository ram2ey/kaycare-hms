using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace KayCare.Tests.Infrastructure;

[Collection("Integration")]
public class HealthCheckTests : IClassFixture<MediCloudWebAppFactory>
{
    private readonly MediCloudWebAppFactory _factory;

    public HealthCheckTests(MediCloudWebAppFactory factory) => _factory = factory;

    // Render's health checker (and any load balancer) hits /health over the platform's own
    // *.onrender.com hostname, e.g. kaycare-hms-api.onrender.com - a 3-part dot-separated host
    // that TenantResolutionMiddleware's subdomain heuristic would otherwise parse as a tenant
    // code, fail to resolve, and 404. Health checks must never depend on tenant resolution.
    [Fact]
    public async Task Health_WithOnrenderStyleHost_Returns200NotTenantNotFound()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });

        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Host = "kaycare-hms-api.onrender.com";

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
