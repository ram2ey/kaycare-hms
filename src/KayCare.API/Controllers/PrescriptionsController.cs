using KayCare.Core.Constants;
using KayCare.Core.DTOs.Prescriptions;
using KayCare.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KayCare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PrescriptionsController : ControllerBase
{
    private readonly IPrescriptionService       _prescriptions;
    private readonly IPrescriptionReportService _report;

    public PrescriptionsController(IPrescriptionService prescriptions, IPrescriptionReportService report)
    {
        _prescriptions = prescriptions;
        _report        = report;
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

    /// <summary>Download a PDF prescription slip.</summary>
    [HttpGet("{id:guid}/report")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetReport(Guid id, CancellationToken ct)
    {
        var pdf = await _report.GeneratePrescriptionReportAsync(id, ct);
        if (pdf == null) return NotFound();
        var shortRef = id.ToString("N")[..8].ToUpperInvariant();
        return File(pdf, "application/pdf", $"Prescription-{shortRef}.pdf");
    }

    /// <summary>Pharmacist partially dispenses a prescription by item quantities.</summary>
    [HttpPost("{id:guid}/partial-dispense")]
    [Authorize(Roles = $"{Roles.Pharmacist},{Roles.Admin},{Roles.SuperAdmin}")]
    [ProducesResponseType(typeof(PrescriptionDetailResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> PartialDispense(Guid id, [FromBody] PartialDispenseRequest request, CancellationToken ct)
    {
        var result = await _prescriptions.PartialDispenseAsync(id, request, ct);
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
