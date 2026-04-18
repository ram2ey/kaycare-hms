using KayCare.Core.Constants;
using KayCare.Core.DTOs.Billing;
using KayCare.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KayCare.API.Controllers;

[ApiController]
[Route("api/service-catalog")]
[Authorize]
public class ServiceCatalogController : ControllerBase
{
    private readonly IServiceCatalogService _catalog;

    public ServiceCatalogController(IServiceCatalogService catalog)
    {
        _catalog = catalog;
    }

    /// <summary>All catalog items. Pass activeOnly=false to include inactive items (Admin/SuperAdmin only).</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = true, CancellationToken ct = default)
    {
        // Non-admins can only see active items
        var isAdmin = User.IsInRole(Roles.Admin) || User.IsInRole(Roles.SuperAdmin);
        if (!isAdmin) activeOnly = true;

        var result = await _catalog.GetAllAsync(activeOnly, ct);
        return Ok(result);
    }

    /// <summary>Single catalog item by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _catalog.GetByIdAsync(id, ct);
        return result == null ? NotFound() : Ok(result);
    }

    /// <summary>Create a new catalog item.</summary>
    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    [ProducesResponseType(typeof(ServiceCatalogItemResponse), 201)]
    public async Task<IActionResult> Create([FromBody] SaveServiceCatalogItemRequest request, CancellationToken ct)
    {
        var result = await _catalog.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.ServiceCatalogItemId }, result);
    }

    /// <summary>Update an existing catalog item.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    [ProducesResponseType(typeof(ServiceCatalogItemResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveServiceCatalogItemRequest request, CancellationToken ct)
    {
        var result = await _catalog.UpdateAsync(id, request, ct);
        return Ok(result);
    }

    /// <summary>Delete a catalog item.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _catalog.DeleteAsync(id, ct);
        return NoContent();
    }
}
