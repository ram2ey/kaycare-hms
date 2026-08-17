using KayCare.API.Extensions;
using KayCare.Core.Constants;
using KayCare.Core.DTOs.Billing;
using KayCare.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KayCare.API.Controllers;

[ApiController]
[Route("api/bill-templates")]
[Authorize]
public class BillTemplatesController : ControllerBase
{
    private readonly IBillTemplateService _templates;

    public BillTemplatesController(IBillTemplateService templates)
    {
        _templates = templates;
    }

    /// <summary>List bill templates. All authenticated users can read active templates.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool    includeInactive = false,
        [FromQuery] string? category        = null,
        CancellationToken   ct              = default)
    {
        var isAdmin = User.IsAdminOrSuperAdmin();
        // Non-admins can only see active templates
        var result = await _templates.GetAllAsync(isAdmin && includeInactive, category, ct);
        return Ok(result);
    }

    /// <summary>Get a single template by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await _templates.GetByIdAsync(id, ct));

    /// <summary>Create a new bill template (Admin/SuperAdmin).</summary>
    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    [ProducesResponseType(typeof(BillTemplateResponse), 201)]
    public async Task<IActionResult> Create([FromBody] SaveBillTemplateRequest request, CancellationToken ct)
    {
        var result = await _templates.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.BillTemplateId }, result);
    }

    /// <summary>Update a bill template (Admin/SuperAdmin). Replaces all items.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveBillTemplateRequest request, CancellationToken ct)
        => Ok(await _templates.UpdateAsync(id, request, ct));

    /// <summary>Delete a bill template (Admin/SuperAdmin).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _templates.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>Toggle a template active/inactive (Admin/SuperAdmin).</summary>
    [HttpPut("{id:guid}/toggle-active")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<IActionResult> ToggleActive(Guid id, CancellationToken ct)
        => Ok(await _templates.ToggleActiveAsync(id, ct));
}
