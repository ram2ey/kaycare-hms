using MediCloud.Core.Constants;
using MediCloud.Core.DTOs.Prescriptions;
using MediCloud.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediCloud.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PrescriptionsController : ControllerBase
{
    private readonly IPrescriptionService _prescriptions;

    public PrescriptionsController(IPrescriptionService prescriptions)
    {
        _prescriptions = prescriptions;
    }

    /// <summary>Pharmacist work queue — all Active prescriptions, oldest first.</summary>
    [HttpGet("pending")]
    [Authorize(Roles = $"{Roles.Pharmacist},{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<IActionResult> GetPending(CancellationToken ct)
    {
        var result = await _prescriptions.GetPendingAsync(ct);
        return Ok(result);
    }

    /// <summary>All prescriptions for a patient, newest first.</summary>
    [HttpGet("patient/{patientId:guid}")]
    public async Task<IActionResult> GetPatientHistory(Guid patientId, CancellationToken ct)
    {
        var result = await _prescriptions.GetPatientHistoryAsync(patientId, ct);
        return Ok(result);
    }

    /// <summary>Prescriptions linked to a specific consultation.</summary>
    [HttpGet("consultation/{consultationId:guid}")]
    public async Task<IActionResult> GetByConsultation(Guid consultationId, CancellationToken ct)
    {
        var result = await _prescriptions.GetByConsultationAsync(consultationId, ct);
        return Ok(result);
    }

    /// <summary>Full prescription detail including all medication line items.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PrescriptionDetailResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _prescriptions.GetByIdAsync(id, ct);
        return Ok(result);
    }

    /// <summary>Create a prescription with one or more medication line items. Doctor only.</summary>
    [HttpPost]
    [Authorize(Roles = $"{Roles.Doctor},{Roles.SuperAdmin},{Roles.Admin}")]
    [ProducesResponseType(typeof(PrescriptionDetailResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Create([FromBody] CreatePrescriptionRequest request, CancellationToken ct)
    {
        var result = await _prescriptions.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.PrescriptionId }, result);
    }

    /// <summary>Pharmacist dispenses a prescription. Status: Active → Dispensed.</summary>
    [HttpPost("{id:guid}/dispense")]
    [Authorize(Roles = $"{Roles.Pharmacist},{Roles.Admin},{Roles.SuperAdmin}")]
    [ProducesResponseType(typeof(PrescriptionDetailResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Dispense(Guid id, [FromBody] DispensePrescriptionRequest request, CancellationToken ct)
    {
        var result = await _prescriptions.DispenseAsync(id, request, ct);
        return Ok(result);
    }

    /// <summary>Cancel an active prescription. Status: Active → Cancelled.</summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = $"{Roles.Doctor},{Roles.Admin},{Roles.SuperAdmin}")]
    [ProducesResponseType(typeof(PrescriptionDetailResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var result = await _prescriptions.CancelAsync(id, ct);
        return Ok(result);
    }
}
