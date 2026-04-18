using KayCare.Core.Constants;
using KayCare.Core.DTOs.PrescriptionTemplates;
using KayCare.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KayCare.API.Controllers;

[ApiController]
[Route("api/prescription-templates")]
[Authorize(Roles = $"{Roles.Doctor},{Roles.Admin},{Roles.SuperAdmin}")]
public class PrescriptionTemplatesController : ControllerBase
{
    private readonly IPrescriptionTemplateService _templates;

    public PrescriptionTemplatesController(IPrescriptionTemplateService templates)
    {
        _templates = templates;
    }

    /// <summary>Templates created by the calling user.</summary>
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var result = await _templates.GetMyTemplatesAsync(ct);
        return Ok(result);
    }

    /// <summary>All shared templates in the tenant.</summary>
    [HttpGet("shared")]
    public async Task<IActionResult> GetShared(CancellationToken ct)
    {
        var result = await _templates.GetSharedTemplatesAsync(ct);
        return Ok(result);
    }

    /// <summary>Full template detail with all medication items.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PrescriptionTemplateDetailResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _templates.GetByIdAsync(id, ct);
        return Ok(result);
    }

    /// <summary>Create a new prescription template.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(PrescriptionTemplateDetailResponse), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreatePrescriptionTemplateRequest request, CancellationToken ct)
    {
        var result = await _templates.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.TemplateId }, result);
    }

    /// <summary>Replace a template's name, description, and all its items.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PrescriptionTemplateDetailResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreatePrescriptionTemplateRequest request, CancellationToken ct)
    {
        var result = await _templates.UpdateAsync(id, request, ct);
        return Ok(result);
    }

    /// <summary>Delete a template. Only the creator can delete.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _templates.DeleteAsync(id, ct);
        return NoContent();
    }
}
