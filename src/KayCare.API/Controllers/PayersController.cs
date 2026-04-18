using KayCare.Core.Constants;
using KayCare.Core.DTOs.Billing;
using KayCare.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KayCare.API.Controllers;

[ApiController]
[Route("api/payers")]
[Authorize]
public class PayersController : ControllerBase
{
    private readonly IPayerService _payers;

    public PayersController(IPayerService payers)
    {
        _payers = payers;
    }

    /// <summary>All payers. Pass activeOnly=false to include inactive (Admin/SuperAdmin only).</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = true, CancellationToken ct = default)
    {
        var isAdmin = User.IsInRole(Roles.Admin) || User.IsInRole(Roles.SuperAdmin);
        if (!isAdmin) activeOnly = true;

        var result = await _payers.GetAllAsync(activeOnly, ct);
        return Ok(result);
    }

    /// <summary>Single payer by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _payers.GetByIdAsync(id, ct);
        return result == null ? NotFound() : Ok(result);
    }

    /// <summary>Create a new payer.</summary>
    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    [ProducesResponseType(typeof(PayerResponse), 201)]
    public async Task<IActionResult> Create([FromBody] SavePayerRequest request, CancellationToken ct)
    {
        var result = await _payers.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.PayerId }, result);
    }

    /// <summary>Update an existing payer.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    [ProducesResponseType(typeof(PayerResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] SavePayerRequest request, CancellationToken ct)
    {
        var result = await _payers.UpdateAsync(id, request, ct);
        return Ok(result);
    }

    /// <summary>Delete a payer.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _payers.DeleteAsync(id, ct);
        return NoContent();
    }
}
