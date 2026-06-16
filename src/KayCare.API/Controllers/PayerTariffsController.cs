using KayCare.Core.Constants;
using KayCare.Core.DTOs.Billing;
using KayCare.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KayCare.API.Controllers;

[ApiController]
[Route("api/payer-tariffs")]
[Authorize]
public class PayerTariffsController : ControllerBase
{
    private readonly IPayerTariffService _payerTariffs;

    public PayerTariffsController(IPayerTariffService payerTariffs)
    {
        _payerTariffs = payerTariffs;
    }

    /// <summary>Get payer tariffs, optionally filtered by payer or service item.</summary>
    [HttpGet]
    public async Task<IActionResult> GetTariffs(
        [FromQuery] Guid? payerId,
        [FromQuery] Guid? serviceCatalogItemId,
        CancellationToken ct)
    {
        if (payerId.HasValue)
        {
            var result = await _payerTariffs.GetByPayerAsync(payerId.Value, ct);
            return Ok(result);
        }

        if (serviceCatalogItemId.HasValue)
        {
            var result = await _payerTariffs.GetByServiceItemAsync(serviceCatalogItemId.Value, ct);
            return Ok(result);
        }

        // Return all if no filters specified
        var all = await _payerTariffs.GetAllAsync(activeOnly: false, ct);
        return Ok(all);
    }

    /// <summary>Create or update a tariff (upsert).</summary>
    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    [ProducesResponseType(typeof(PayerTariffResponse), 200)]
    public async Task<IActionResult> Upsert([FromBody] SavePayerTariffRequest request, CancellationToken ct)
    {
        var result = await _payerTariffs.UpsertAsync(request, ct);
        return Ok(result);
    }

    /// <summary>Update an existing tariff by ID.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    [ProducesResponseType(typeof(PayerTariffResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] SavePayerTariffRequest request, CancellationToken ct)
    {
        var result = await _payerTariffs.UpdateAsync(id, request, ct);
        return Ok(result);
    }

    /// <summary>Delete a tariff.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _payerTariffs.DeleteAsync(id, ct);
        return NoContent();
    }
}
