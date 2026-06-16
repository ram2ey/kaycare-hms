using KayCare.Core.Constants;
using KayCare.Core.DTOs.Radiology;
using KayCare.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace KayCare.API.Controllers;

[ApiController]
[Route("api/radiology-orders")]
[Authorize]
public class RadiologyOrdersController : ControllerBase
{
    private readonly IRadiologyOrderService  _radiology;
    private readonly IRadiologyReportService _radiologyReport;

    public RadiologyOrdersController(IRadiologyOrderService radiology, IRadiologyReportService radiologyReport)
    {
        _radiology       = radiology;
        _radiologyReport = radiologyReport;
    }

    /// <summary>All active imaging procedures in the catalog (for order dropdowns).</summary>
    [HttpGet("catalog")]
    [ProducesResponseType(typeof(IReadOnlyList<ImagingProcedureResponse>), 200)]
    public async Task<IActionResult> GetCatalog(CancellationToken ct)
        => Ok(await _radiology.GetProcedureCatalogAsync(ct));

    /// <summary>Full catalog including inactive — Admin/SuperAdmin only.</summary>
    [HttpGet("catalog/all")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    [ProducesResponseType(typeof(IReadOnlyList<ImagingProcedureResponse>), 200)]
    public async Task<IActionResult> GetFullCatalog(CancellationToken ct)
        => Ok(await _radiology.GetFullProcedureCatalogAsync(ct));

    /// <summary>Create a new imaging procedure in the catalog.</summary>
    [HttpPost("catalog")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    [ProducesResponseType(typeof(ImagingProcedureResponse), 201)]
    public async Task<IActionResult> CreateProcedure([FromBody] SaveImagingProcedureRequest req, CancellationToken ct)
    {
        var result = await _radiology.CreateProcedureAsync(req, ct);
        return StatusCode(201, result);
    }

    /// <summary>Update an existing imaging procedure in the catalog.</summary>
    [HttpPut("catalog/{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    [ProducesResponseType(typeof(ImagingProcedureResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateProcedure(Guid id, [FromBody] SaveImagingProcedureRequest req, CancellationToken ct)
    {
        var result = await _radiology.UpdateProcedureAsync(id, req, ct);
        return Ok(result);
    }

    /// <summary>Toggle active status of an imaging procedure in the catalog.</summary>
    [HttpPatch("catalog/{id:guid}/toggle")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    [ProducesResponseType(typeof(ImagingProcedureResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ToggleProcedure(Guid id, CancellationToken ct)
        => Ok(await _radiology.ToggleProcedureActiveAsync(id, ct));

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
        => Ok(await _radiology.GetStatsAsync(ct));

    [HttpGet("worklist")]
    public async Task<IActionResult> GetWorklist(
        [FromQuery] DateOnly? date,
        [FromQuery] string?   status,
        CancellationToken ct)
    {
        var d = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return Ok(await _radiology.GetWorklistAsync(d, status, ct));
    }

    [HttpGet("patient/{patientId:guid}")]
    public async Task<IActionResult> GetByPatient(Guid patientId, CancellationToken ct)
        => Ok(await _radiology.GetByPatientAsync(patientId, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var order = await _radiology.GetByIdAsync(id, ct);
        return order == null ? NotFound() : Ok(order);
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Doctor},{Roles.Receptionist}")]
    public async Task<IActionResult> PlaceOrder([FromBody] CreateRadiologyOrderRequest request, CancellationToken ct)
    {
        var order = await _radiology.PlaceOrderAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = order.RadiologyOrderId }, order);
    }

    [HttpPost("items/{itemId:guid}/acquire")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.LabTechnician}")]
    public async Task<IActionResult> MarkAcquired(Guid itemId, CancellationToken ct)
        => Ok(await _radiology.MarkAcquiredAsync(itemId, null, ct));

    [HttpPost("items/{itemId:guid}/pacs-upload")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.LabTechnician}")]
    public async Task<IActionResult> UploadPacsStudy(Guid itemId, [FromForm] Microsoft.AspNetCore.Http.IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No scan file uploaded." });

        var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var pacsDir = Path.Combine(wwwrootPath, "pacs-studies");
        if (!Directory.Exists(pacsDir))
        {
            Directory.CreateDirectory(pacsDir);
        }

        var filePath = Path.Combine(pacsDir, $"{itemId}.png");
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream, ct);
        }

        var pacsUrl = $"/pacs-studies/{itemId}.png";
        var result = await _radiology.MarkAcquiredAsync(itemId, pacsUrl, ct);
        return Ok(result);
    }

    [HttpPost("items/{itemId:guid}/report")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Doctor}")]
    public async Task<IActionResult> EnterReport(Guid itemId, [FromBody] RadiologyReportRequest request, CancellationToken ct)
        => Ok(await _radiology.EnterReportAsync(itemId, request, ct));

    [HttpPost("items/{itemId:guid}/sign")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Doctor}")]
    public async Task<IActionResult> SignItem(Guid itemId, CancellationToken ct)
        => Ok(await _radiology.SignItemAsync(itemId, ct));

    [HttpGet("{id:guid}/report")]
    public async Task<IActionResult> GetReport(Guid id, CancellationToken ct)
    {
        var pdf = await _radiologyReport.GenerateReportAsync(id, ct);
        if (pdf == null) return NotFound();
        return File(pdf, "application/pdf", $"radiology-report-{id:N}.pdf");
    }
}
