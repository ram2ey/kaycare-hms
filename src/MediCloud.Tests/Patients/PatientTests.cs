using System.Net;
using System.Text.Json;
using MediCloud.Core.DTOs.Patients;
using MediCloud.Tests.Infrastructure;

namespace MediCloud.Tests.Patients;

/// <summary>
/// Integration tests for the Patients API.
/// Covers: registration, MRN format, sequential numbering, search, and RBAC.
/// </summary>
[Collection("Integration")]
public class PatientTests : IClassFixture<MediCloudWebAppFactory>
{
    private readonly MediCloudWebAppFactory _factory;

    public PatientTests(MediCloudWebAppFactory factory) => _factory = factory;

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static CreatePatientRequest MinimalPatient(string suffix = "") => new()
    {
        FirstName   = "John" + suffix,
        LastName    = "Doe" + suffix,
        DateOfBirth = new DateOnly(1985, 6, 15),
        Gender      = "Male",
    };

    // ── Registration ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterPatient_Returns201WithMrn()
    {
        var client = await _factory.CreateAdminClientAsync(_factory.TenantA);

        var resp = await client.PostAsJsonAsync("/api/patients", MinimalPatient());

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var mrn  = body.GetProperty("mrn").GetString();

        Assert.False(string.IsNullOrEmpty(mrn));
        // Format: MRN-{YEAR}-{5-digit}
        Assert.Matches(@"^MRN-\d{4}-\d{5}$", mrn);
    }

    [Fact]
    public async Task RegisterPatient_MrnContainsCurrentYear()
    {
        var client = await _factory.CreateAdminClientAsync(_factory.TenantA);

        var resp = await client.PostAsJsonAsync("/api/patients", MinimalPatient());
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var mrn  = body.GetProperty("mrn").GetString()!;

        var year = mrn.Split('-')[1];
        Assert.Equal(DateTime.UtcNow.Year.ToString(), year);
    }

    [Fact]
    public async Task RegisterPatient_MrnSequentialWithinTenant()
    {
        var client = await _factory.CreateAdminClientAsync(_factory.TenantA);

        var resp1 = await client.PostAsJsonAsync("/api/patients", MinimalPatient("SeqA"));
        var resp2 = await client.PostAsJsonAsync("/api/patients", MinimalPatient("SeqB"));

        Assert.Equal(HttpStatusCode.Created, resp1.StatusCode);
        Assert.Equal(HttpStatusCode.Created, resp2.StatusCode);

        var mrn1 = (await resp1.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("mrn").GetString()!;
        var mrn2 = (await resp2.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("mrn").GetString()!;

        var seq1 = int.Parse(mrn1.Split('-')[2]);
        var seq2 = int.Parse(mrn2.Split('-')[2]);

        Assert.Equal(seq1 + 1, seq2);
    }

    // ── Get by ID ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPatient_Returns200WithCorrectData()
    {
        var client = await _factory.CreateAdminClientAsync(_factory.TenantA);

        var createResp = await client.PostAsJsonAsync("/api/patients", new CreatePatientRequest
        {
            FirstName   = "Alice",
            LastName    = "Smith",
            DateOfBirth = new DateOnly(1990, 3, 20),
            Gender      = "Female",
        });
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);

        var created    = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var patientId  = created.GetProperty("patientId").GetString()!;

        var getResp = await client.GetAsync($"/api/patients/{patientId}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);

        var body = await getResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Alice", body.GetProperty("firstName").GetString());
        Assert.Equal("Smith", body.GetProperty("lastName").GetString());
    }

    [Fact]
    public async Task GetPatient_NotFound_Returns404()
    {
        var client = await _factory.CreateAdminClientAsync(_factory.TenantA);

        var resp = await client.GetAsync($"/api/patients/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ── Search ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SearchPatients_ByLastName_ReturnsMatchingResults()
    {
        var client     = await _factory.CreateAdminClientAsync(_factory.TenantA);
        var uniqueName = "Unique" + Guid.NewGuid().ToString("N")[..6];

        await client.PostAsJsonAsync("/api/patients", new CreatePatientRequest
        {
            FirstName   = "Search",
            LastName    = uniqueName,
            DateOfBirth = new DateOnly(1975, 1, 1),
            Gender      = "Male",
        });

        var searchResp = await client.GetAsync($"/api/patients?query={uniqueName}");
        Assert.Equal(HttpStatusCode.OK, searchResp.StatusCode);

        var body  = await searchResp.Content.ReadFromJsonAsync<JsonElement>();
        var items = body.GetProperty("items");
        Assert.True(items.GetArrayLength() >= 1);

        // Verify the returned patient has the correct name
        var first = items[0];
        Assert.Equal(uniqueName, first.GetProperty("lastName").GetString());
    }

    // ── Authorization ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterPatient_WithoutAuth_Returns401()
    {
        var client = _factory.CreateAnonymousClientForTenant(_factory.TenantA);

        var resp = await client.PostAsJsonAsync("/api/patients", MinimalPatient());

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }
}
