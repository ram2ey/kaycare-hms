using KayCare.Core.Constants;
using KayCare.Core.DTOs.Billing;
using KayCare.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KayCare.API.Controllers;

[ApiController]
[Route("api/billing-reports")]
[Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
public class BillingReportsController : ControllerBase
{
    private readonly IBillingReportsService _reports;

    public BillingReportsController(IBillingReportsService reports)
    {
        _reports = reports;
    }

    /// <summary>AR Aging report — all outstanding bills grouped into 0-30, 31-60, 61-90, 90+ day buckets.</summary>
    [HttpGet("ar-aging")]
    [ProducesResponseType(typeof(ArAgingReport), 200)]
    public async Task<IActionResult> GetArAging(CancellationToken ct)
    {
        var result = await _reports.GetArAgingAsync(ct);
        return Ok(result);
    }

    /// <summary>Revenue dashboard — headline metrics, monthly trend, by-payer breakdown.</summary>
    [HttpGet("revenue-dashboard")]
    [ProducesResponseType(typeof(RevenueDashboardResponse), 200)]
    public async Task<IActionResult> GetRevenueDashboard(CancellationToken ct)
    {
        var result = await _reports.GetRevenueDashboardAsync(ct);
        return Ok(result);
    }
}
