using System.Net;
using System.Text.Json;
using MediCloud.Core.DTOs.Patients;
using MediCloud.Tests.Infrastructure;

namespace MediCloud.Tests.Billing;

/// <summary>
/// Integration tests for the Billing API.
/// Covers: INV format, state machine transitions, payments, and overpayment guard.
/// </summary>
[Collection("Integration")]
public class BillingTests : IClassFixture<MediCloudWebAppFactory>
{
    private readonly MediCloudWebAppFactory _factory;

    public BillingTests(MediCloudWebAppFactory factory) => _factory = factory;

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Creates a patient and returns their ID as a string.</summary>
    private async Task<string> CreatePatientAsync(HttpClient client)
    {
        var resp = await client.PostAsJsonAsync("/api/patients", new CreatePatientRequest
        {
            FirstName   = "Bill",
            LastName    = "Patient-" + Guid.NewGuid().ToString("N")[..6],
            DateOfBirth = new DateOnly(1980, 1, 1),
            Gender      = "Male",
        });
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("patientId").GetString()!;
    }

    /// <summary>Creates a draft bill for a patient and returns its ID and INV number.</summary>
    private async Task<(string BillId, string InvNumber)> CreateBillAsync(
        HttpClient client, string patientId, decimal unitPrice = 200m)
    {
        var resp = await client.PostAsJsonAsync("/api/bills", new
        {
            patientId,
            items = new[] { new { description = "Consultation fee", quantity = 1, unitPrice } },
        });
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return (body.GetProperty("billId").GetString()!, body.GetProperty("billNumber").GetString()!);
    }

    // ── INV number format ─────────────────────────────────────────────────────

    [Fact]
    public async Task CreateBill_ReturnsInvNumberInCorrectFormat()
    {
        var client    = await _factory.CreateAdminClientAsync(_factory.TenantA);
        var patientId = await CreatePatientAsync(client);

        var (_, invNumber) = await CreateBillAsync(client, patientId);

        Assert.Matches(@"^INV-\d{4}-\d{5}$", invNumber);
    }

    [Fact]
    public async Task CreateBill_InvNumberContainsCurrentYear()
    {
        var client    = await _factory.CreateAdminClientAsync(_factory.TenantA);
        var patientId = await CreatePatientAsync(client);

        var (_, invNumber) = await CreateBillAsync(client, patientId);

        var year = invNumber.Split('-')[1];
        Assert.Equal(DateTime.UtcNow.Year.ToString(), year);
    }

    [Fact]
    public async Task CreateBill_InvNumbersAreSequential()
    {
        var client    = await _factory.CreateAdminClientAsync(_factory.TenantA);
        var patientId = await CreatePatientAsync(client);

        var (_, inv1) = await CreateBillAsync(client, patientId, 100m);
        var (_, inv2) = await CreateBillAsync(client, patientId, 150m);

        var seq1 = int.Parse(inv1.Split('-')[2]);
        var seq2 = int.Parse(inv2.Split('-')[2]);
        Assert.Equal(seq1 + 1, seq2);
    }

    // ── Initial state ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateBill_StartsInDraftStatus()
    {
        var client    = await _factory.CreateAdminClientAsync(_factory.TenantA);
        var patientId = await CreatePatientAsync(client);
        var (billId, _) = await CreateBillAsync(client, patientId);

        var getResp = await client.GetAsync($"/api/bills/{billId}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);

        var body = await getResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Draft", body.GetProperty("status").GetString());
    }

    // ── State machine: valid transitions ─────────────────────────────────────

    [Fact]
    public async Task IssueBill_TransitionsDraftToIssued()
    {
        var client    = await _factory.CreateAdminClientAsync(_factory.TenantA);
        var patientId = await CreatePatientAsync(client);
        var (billId, _) = await CreateBillAsync(client, patientId);

        var issueResp = await client.PostAsync($"/api/bills/{billId}/issue", null);
        Assert.Equal(HttpStatusCode.OK, issueResp.StatusCode);

        var body = await issueResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Issued", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task AddPayment_PartialAmount_TransitionsToPartiallyPaid()
    {
        var client    = await _factory.CreateAdminClientAsync(_factory.TenantA);
        var patientId = await CreatePatientAsync(client);
        var (billId, _) = await CreateBillAsync(client, patientId, unitPrice: 300m);

        await client.PostAsync($"/api/bills/{billId}/issue", null);

        var payResp = await client.PostAsJsonAsync($"/api/bills/{billId}/payments", new
        {
            amount        = 100m,
            paymentMethod = "Cash",
        });
        Assert.Equal(HttpStatusCode.OK, payResp.StatusCode);

        var body = await payResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("PartiallyPaid", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task AddPayment_FullAmount_TransitionsToPaid()
    {
        var client    = await _factory.CreateAdminClientAsync(_factory.TenantA);
        var patientId = await CreatePatientAsync(client);
        var (billId, _) = await CreateBillAsync(client, patientId, unitPrice: 250m);

        await client.PostAsync($"/api/bills/{billId}/issue", null);

        var payResp = await client.PostAsJsonAsync($"/api/bills/{billId}/payments", new
        {
            amount        = 250m,
            paymentMethod = "Mobile Money",
        });
        Assert.Equal(HttpStatusCode.OK, payResp.StatusCode);

        var body = await payResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Paid", body.GetProperty("status").GetString());
    }

    // ── State machine: invalid transitions ───────────────────────────────────

    [Fact]
    public async Task IssueBill_AlreadyIssued_Returns409()
    {
        var client    = await _factory.CreateAdminClientAsync(_factory.TenantA);
        var patientId = await CreatePatientAsync(client);
        var (billId, _) = await CreateBillAsync(client, patientId);

        await client.PostAsync($"/api/bills/{billId}/issue", null);         // first issue
        var resp = await client.PostAsync($"/api/bills/{billId}/issue", null); // duplicate

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact]
    public async Task AddPayment_ToDraftBill_Returns400()
    {
        var client    = await _factory.CreateAdminClientAsync(_factory.TenantA);
        var patientId = await CreatePatientAsync(client);
        var (billId, _) = await CreateBillAsync(client, patientId, unitPrice: 100m);

        // Draft bills cannot accept payments
        var payResp = await client.PostAsJsonAsync($"/api/bills/{billId}/payments", new
        {
            amount        = 50m,
            paymentMethod = "Cash",
        });

        Assert.Equal(HttpStatusCode.BadRequest, payResp.StatusCode);
    }

    // ── Overpayment guard ─────────────────────────────────────────────────────

    [Fact]
    public async Task AddPayment_ExceedsBalanceDue_Returns400()
    {
        var client    = await _factory.CreateAdminClientAsync(_factory.TenantA);
        var patientId = await CreatePatientAsync(client);
        var (billId, _) = await CreateBillAsync(client, patientId, unitPrice: 100m);

        await client.PostAsync($"/api/bills/{billId}/issue", null);

        var payResp = await client.PostAsJsonAsync($"/api/bills/{billId}/payments", new
        {
            amount        = 999m, // more than the 100.00 balance
            paymentMethod = "Cash",
        });

        Assert.Equal(HttpStatusCode.BadRequest, payResp.StatusCode);
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CancelBill_FromDraft_TransitionsToCancelled()
    {
        var client    = await _factory.CreateAdminClientAsync(_factory.TenantA);
        var patientId = await CreatePatientAsync(client);
        var (billId, _) = await CreateBillAsync(client, patientId);

        var cancelResp = await client.PostAsync($"/api/bills/{billId}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, cancelResp.StatusCode);

        var body = await cancelResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Cancelled", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task CancelBill_AlreadyPaid_Returns409()
    {
        var client    = await _factory.CreateAdminClientAsync(_factory.TenantA);
        var patientId = await CreatePatientAsync(client);
        var (billId, _) = await CreateBillAsync(client, patientId, unitPrice: 50m);

        await client.PostAsync($"/api/bills/{billId}/issue", null);
        await client.PostAsJsonAsync($"/api/bills/{billId}/payments", new
        {
            amount = 50m, paymentMethod = "Cash",
        });

        var cancelResp = await client.PostAsync($"/api/bills/{billId}/cancel", null);
        Assert.Equal(HttpStatusCode.Conflict, cancelResp.StatusCode);
    }
}
