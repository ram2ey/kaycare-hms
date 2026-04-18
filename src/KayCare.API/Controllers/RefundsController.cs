using KayCare.Core.Constants;
using KayCare.Core.DTOs.Billing;
using KayCare.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KayCare.API.Controllers;

[ApiController]
[Route("api/refunds")]
[Authorize]
public class RefundsController : ControllerBase
{
    private readonly IRefundService _refunds;

    public RefundsController(IRefundService refunds)
    {
        _refunds = refunds;
    }

    /// <summary>List refunds with optional filters: status, billId, patientId.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] Guid?   billId,
        [FromQuery] Guid?   patientId,
        CancellationToken   ct)
    {
        var result = await _refunds.GetAllAsync(status, billId, patientId, ct);
        return Ok(result);
    }

    /// <summary>Get a single refund by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _refunds.GetByIdAsync(id, ct);
        return result == null ? NotFound() : Ok(result);
    }

    /// <summary>Create a new refund request (Pending).</summary>
    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin},{Roles.BillingOfficer}")]
    [ProducesResponseType(typeof(RefundResponse), 201)]
    public async Task<IActionResult> Create([FromBody] CreateRefundRequest request, CancellationToken ct)
    {
        var result = await _refunds.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.RefundId }, result);
    }

    /// <summary>Mark a Pending refund as Processed (money returned to patient).</summary>
    [HttpPut("{id:guid}/process")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    [ProducesResponseType(typeof(RefundResponse), 200)]
    public async Task<IActionResult> Process(Guid id, CancellationToken ct)
    {
        var result = await _refunds.ProcessAsync(id, ct);
        return Ok(result);
    }

    /// <summary>Cancel a Pending refund.</summary>
    [HttpPut("{id:guid}/cancel")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    [ProducesResponseType(typeof(RefundResponse), 200)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var result = await _refunds.CancelAsync(id, ct);
        return Ok(result);
    }
}
