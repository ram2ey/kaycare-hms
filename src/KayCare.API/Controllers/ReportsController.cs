using KayCare.Core.Constants;
using KayCare.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KayCare.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin},{Roles.Doctor}")]
public class ReportsController : ControllerBase
{
    private readonly IReportsService _reports;

    public ReportsController(IReportsService reports) => _reports = reports;

    [HttpGet("clinical")]
    public async Task<IActionResult> GetClinical(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct)
    {
        var (f, t) = DefaultRange(from, to);
        return Ok(await _reports.GetClinicalAsync(f, t, ct));
    }

    [HttpGet("inpatient")]
    public async Task<IActionResult> GetInpatient(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct)
    {
        var (f, t) = DefaultRange(from, to);
        return Ok(await _reports.GetInpatientAsync(f, t, ct));
    }

    [HttpGet("lab")]
    public async Task<IActionResult> GetLab(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct)
    {
        var (f, t) = DefaultRange(from, to);
        return Ok(await _reports.GetLabAsync(f, t, ct));
    }

    [HttpGet("referrals")]
    public async Task<IActionResult> GetReferrals(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct)
    {
        var (f, t) = DefaultRange(from, to);
        return Ok(await _reports.GetReferralsAsync(f, t, ct));
    }

    [HttpGet("pharmacy")]
    public async Task<IActionResult> GetPharmacy(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct)
    {
        var (f, t) = DefaultRange(from, to);
        return Ok(await _reports.GetPharmacyAsync(f, t, ct));
    }

    private static (DateOnly from, DateOnly to) DefaultRange(DateOnly? from, DateOnly? to)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return (from ?? today.AddDays(-29), to ?? today);
    }
}
