using KayCare.Core.Constants;
using KayCare.Core.DTOs.Tenants;
using KayCare.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KayCare.API.Controllers;

[ApiController]
[Route("api/tenants")]
[Authorize(Roles = Roles.SuperAdmin)]
public class TenantsController(ITenantService tenants) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await tenants.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var tenant = await tenants.GetByIdAsync(id, ct);
        return tenant is null ? NotFound() : Ok(tenant);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTenantRequest req, CancellationToken ct)
    {
        var result = await tenants.CreateAsync(req, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.TenantId }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTenantRequest req, CancellationToken ct)
        => Ok(await tenants.UpdateAsync(id, req, ct));

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
        => Ok(await tenants.SetActiveAsync(id, true, ct));

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
        => Ok(await tenants.SetActiveAsync(id, false, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await tenants.DeleteAsync(id, ct);
        return NoContent();
    }
}
