using KayCare.Core.Constants;
using KayCare.Core.DTOs.Billing;
using KayCare.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KayCare.API.Controllers;

[ApiController]
[Route("api/insurance-claims")]
[Authorize]
public class InsuranceClaimsController : ControllerBase
{
    private readonly IInsuranceClaimService _claims;

    public InsuranceClaimsController(IInsuranceClaimService claims)
    {
        _claims = claims;
    }

    /// <summary>List claims with optional filters: status, payerId, patientId.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] Guid?   payerId,
        [FromQuery] Guid?   patientId,
        CancellationToken   ct)
    {
        var result = await _claims.GetAllAsync(status, payerId, patientId, ct);
        return Ok(result);
    }

    /// <summary>Get a single claim by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _claims.GetByIdAsync(id, ct);
        return result == null ? NotFound() : Ok(result);
    }

    /// <summary>Create a new insurance claim from a bill.</summary>
    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin},{Roles.Receptionist}")]
    [ProducesResponseType(typeof(InsuranceClaimResponse), 201)]
    public async Task<IActionResult> Create([FromBody] CreateClaimRequest request, CancellationToken ct)
    {
        var result = await _claims.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.ClaimId }, result);
    }

    /// <summary>Mark claim as submitted (sent to insurer).</summary>
    [HttpPut("{id:guid}/submit")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin},{Roles.Receptionist}")]
    [ProducesResponseType(typeof(InsuranceClaimResponse), 200)]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
    {
        var result = await _claims.SubmitAsync(id, ct);
        return Ok(result);
    }

    /// <summary>Record insurer approval and auto-create payment on bill.</summary>
    [HttpPut("{id:guid}/approve")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    [ProducesResponseType(typeof(InsuranceClaimResponse), 200)]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveClaimRequest request, CancellationToken ct)
    {
        var result = await _claims.ApproveAsync(id, request, ct);
        return Ok(result);
    }

    /// <summary>Record insurer rejection.</summary>
    [HttpPut("{id:guid}/reject")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    [ProducesResponseType(typeof(InsuranceClaimResponse), 200)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectClaimRequest request, CancellationToken ct)
    {
        var result = await _claims.RejectAsync(id, request, ct);
        return Ok(result);
    }

    /// <summary>Cancel a Draft or Submitted claim.</summary>
    [HttpPut("{id:guid}/cancel")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    [ProducesResponseType(typeof(InsuranceClaimResponse), 200)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var result = await _claims.CancelAsync(id, ct);
        return Ok(result);
    }

    /// <summary>Download claim as A4 PDF.</summary>
    [HttpGet("{id:guid}/report")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Report(Guid id, CancellationToken ct)
    {
        var pdf = await _claims.GenerateClaimPdfAsync(id, ct);
        if (pdf == null) return NotFound();
        return File(pdf, "application/pdf", $"claim-{id}.pdf");
    }
}
