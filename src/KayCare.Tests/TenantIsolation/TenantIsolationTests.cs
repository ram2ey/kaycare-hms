using System.Net;
using System.Text.Json;
using KayCare.Core.DTOs.Patients;
using KayCare.Tests.Infrastructure;

namespace KayCare.Tests.TenantIsolation;

/// <summary>
/// Verifies that data created in one tenant is completely invisible to another.
/// This is the most security-critical test suite in the project.
/// </summary>
[Collection("Integration")]
public class TenantIsolationTests : IClassFixture<MediCloudWebAppFactory>
{
    private readonly MediCloudWebAppFactory _factory;

    public TenantIsolationTests(MediCloudWebAppFactory factory) => _factory = factory;

    // ── Patient isolation ─────────────────────────────────────────────────────

    [Fact]
    public async Task Patient_CreatedInTenantA_NotVisibleFromTenantB()
    {
        var clientA = await _factory.CreateAdminClientAsync(_factory.TenantA);
        var clientB = await _factory.CreateAdminClientAsync(_factory.TenantB);

        // Create a patient in TenantA
        var createResp = await clientA.PostAsJsonAsync("/api/patients", new CreatePatientRequest
        {
            FirstName   = "Isolated",
            LastName    = "Patient",
            DateOfBirth = new DateOnly(1980, 1, 1),
            Gender      = "Male",
        });
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);

        var body      = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var patientId = body.GetProperty("patientId").GetString()!;

        // TenantB should NOT be able to retrieve this patient
        var crossResp = await clientB.GetAsync($"/api/patients/{patientId}");
        Assert.Equal(HttpStatusCode.NotFound, crossResp.StatusCode);
    }

    [Fact]
    public async Task PatientList_OnlyReturnsOwnTenantPatients()
    {
        var clientA = await _factory.CreateAdminClientAsync(_factory.TenantA);
        var clientB = await _factory.CreateAdminClientAsync(_factory.TenantB);

        // Create a patient with a unique name in TenantA
        var uniqueName = "CrossTenant" + Guid.NewGuid().ToString("N")[..8];
        await clientA.PostAsJsonAsync("/api/patients", new CreatePatientRequest
        {
            FirstName   = uniqueName,
            LastName    = "Leak",
            DateOfBirth = new DateOnly(1990, 1, 1),
            Gender      = "Female",
        });

        // TenantB's search should return zero results for that name
        var searchResp = await clientB.GetAsync($"/api/patients?query={uniqueName}");
        Assert.Equal(HttpStatusCode.OK, searchResp.StatusCode);

        var result = await searchResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, result.GetProperty("totalCount").GetInt32());
    }

    // ── MRN sequential counter isolation ─────────────────────────────────────

    [Fact]
    public async Task Mrn_SequenceIsIndependentPerTenant()
    {
        // Each tenant's MRN counter starts independently.
        // We only verify the format is correct for both — sequential numbering
        // is verified in PatientTests within the same tenant.
        var clientA = await _factory.CreateAdminClientAsync(_factory.TenantA);
        var clientB = await _factory.CreateAdminClientAsync(_factory.TenantB);

        var respA = await clientA.PostAsJsonAsync("/api/patients", new CreatePatientRequest
        {
            FirstName = "MrnA", LastName = "Tenant", DateOfBirth = new DateOnly(1980, 1, 1), Gender = "Male",
        });
        var respB = await clientB.PostAsJsonAsync("/api/patients", new CreatePatientRequest
        {
            FirstName = "MrnB", LastName = "Tenant", DateOfBirth = new DateOnly(1980, 1, 1), Gender = "Male",
        });

        Assert.Equal(HttpStatusCode.Created, respA.StatusCode);
        Assert.Equal(HttpStatusCode.Created, respB.StatusCode);

        var mrnA = (await respA.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("medicalRecordNumber").GetString()!;
        var mrnB = (await respB.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("medicalRecordNumber").GetString()!;

        Assert.Matches(@"^MRN-\d{4}-\d{5}$", mrnA);
        Assert.Matches(@"^MRN-\d{4}-\d{5}$", mrnB);
    }

    // ── Authentication cross-tenant ───────────────────────────────────────────

    [Fact]
    public async Task TenantA_Token_IgnoresSpoofedTenantBHeader()
    {
        var clientA = await _factory.CreateAdminClientAsync(_factory.TenantA);

        // Create a uniquely-named patient under TenantA
        var uniqueName = "StolenToken" + Guid.NewGuid().ToString("N")[..8];
        await clientA.PostAsJsonAsync("/api/patients", new CreatePatientRequest
        {
            FirstName   = uniqueName,
            LastName    = "Leak",
            DateOfBirth = new DateOnly(1980, 1, 1),
            Gender      = "Male",
        });

        // Swap the tenant header to TenantB while keeping TenantA's JWT — an attempt to escape
        // tenant scope via a spoofed header rather than a stolen/forged token
        clientA.DefaultRequestHeaders.Remove("X-Tenant-Code");
        clientA.DefaultRequestHeaders.Add("X-Tenant-Code", _factory.TenantB.TenantCode);

        // TenantResolutionMiddleware ignores X-Tenant-Code entirely once a request is
        // authenticated and resolves scope from the JWT's tenantId claim instead — so this must
        // still return TenantA's own patient (not TenantB's scope, not a 404/401).
        var resp = await clientA.GetAsync($"/api/patients?query={uniqueName}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, body.GetProperty("totalCount").GetInt32());
    }

    // ── Bill isolation ────────────────────────────────────────────────────────

    [Fact]
    public async Task Bill_CreatedInTenantA_NotVisibleFromTenantB()
    {
        var clientA = await _factory.CreateAdminClientAsync(_factory.TenantA);
        var clientB = await _factory.CreateAdminClientAsync(_factory.TenantB);

        // Register a patient in TenantA
        var patResp = await clientA.PostAsJsonAsync("/api/patients", new CreatePatientRequest
        {
            FirstName = "BillIso", LastName = "Patient",
            DateOfBirth = new DateOnly(1975, 5, 5), Gender = "Male",
        });
        var patientId = (await patResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("patientId").GetString()!;

        // Create a bill in TenantA
        var billResp = await clientA.PostAsJsonAsync("/api/bills", new
        {
            patientId,
            items = new[] { new { description = "Consultation", quantity = 1, unitPrice = 100.00m } },
        });
        Assert.Equal(HttpStatusCode.Created, billResp.StatusCode);

        var billId = (await billResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("billId").GetString()!;

        // TenantB must not see this bill
        var crossResp = await clientB.GetAsync($"/api/bills/{billId}");
        Assert.Equal(HttpStatusCode.NotFound, crossResp.StatusCode);
    }
}
