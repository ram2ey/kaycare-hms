using KayCare.Core.Constants;
using KayCare.Core.DTOs.Inpatient;
using KayCare.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KayCare.API.Controllers;

[ApiController]
[Route("api/wards")]
[Authorize]
public class WardsController : ControllerBase
{
    private readonly IWardService _wards;

    public WardsController(IWardService wards) => _wards = wards;

    // ── Wards ─────────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetWards([FromQuery] bool? activeOnly = true, CancellationToken ct = default)
    {
        var isAdmin = User.IsInRole(Roles.Admin) || User.IsInRole(Roles.SuperAdmin);
        if (!isAdmin) activeOnly = true;
        return Ok(await _wards.GetAllAsync(activeOnly, ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetWard(Guid id, CancellationToken ct)
    {
        var result = await _wards.GetByIdAsync(id, ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    [ProducesResponseType(typeof(WardResponse), 201)]
    public async Task<IActionResult> CreateWard([FromBody] SaveWardRequest request, CancellationToken ct)
    {
        var result = await _wards.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetWard), new { id = result.WardId }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<IActionResult> UpdateWard(Guid id, [FromBody] SaveWardRequest request, CancellationToken ct)
        => Ok(await _wards.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<IActionResult> DeactivateWard(Guid id, CancellationToken ct)
        => Ok(await _wards.DeactivateAsync(id, ct));

    // ── Beds ──────────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/beds")]
    public async Task<IActionResult> GetBeds(Guid id, CancellationToken ct)
        => Ok(await _wards.GetBedsAsync(id, ct));

    [HttpPost("{id:guid}/beds")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    [ProducesResponseType(typeof(BedResponse), 201)]
    public async Task<IActionResult> AddBed(Guid id, [FromBody] SaveBedRequest request, CancellationToken ct)
    {
        var result = await _wards.AddBedAsync(id, request, ct);
        return StatusCode(201, result);
    }

    [HttpPut("beds/{bedId:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<IActionResult> UpdateBed(Guid bedId, [FromBody] SaveBedRequest request, CancellationToken ct)
        => Ok(await _wards.UpdateBedAsync(bedId, request, ct));

    [HttpPatch("beds/{bedId:guid}/status")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin},{Roles.Nurse}")]
    public async Task<IActionResult> UpdateBedStatus(Guid bedId, [FromBody] UpdateBedStatusRequest request, CancellationToken ct)
        => Ok(await _wards.UpdateBedStatusAsync(bedId, request, ct));

    [HttpDelete("beds/{bedId:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<IActionResult> DeleteBed(Guid bedId, CancellationToken ct)
    {
        await _wards.DeleteBedAsync(bedId, ct);
        return NoContent();
    }
}
