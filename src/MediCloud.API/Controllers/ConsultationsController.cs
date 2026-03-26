using MediCloud.Core.Constants;
using MediCloud.Core.DTOs.Consultations;
using MediCloud.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediCloud.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConsultationsController : ControllerBase
{
    private readonly IConsultationService _consultations;

    public ConsultationsController(IConsultationService consultations)
    {
        _consultations = consultations;
    }

    /// <summary>Full consultation history for a patient, newest first.</summary>
    [HttpGet("patient/{patientId:guid}")]
    public async Task<IActionResult> GetPatientHistory(Guid patientId, CancellationToken ct)
    {
        var result = await _consultations.GetPatientHistoryAsync(patientId, ct);
        return Ok(result);
    }

    /// <summary>Retrieve the consultation linked to a specific appointment (if any).</summary>
    [HttpGet("appointment/{appointmentId:guid}")]
    public async Task<IActionResult> GetByAppointment(Guid appointmentId, CancellationToken ct)
    {
        var result = await _consultations.GetByAppointmentAsync(appointmentId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Get a single consultation by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ConsultationDetailResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _consultations.GetByIdAsync(id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Create a consultation for an appointment. Automatically advances the appointment
    /// to InProgress when the applicable status transition is valid.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = $"{Roles.Doctor},{Roles.Nurse},{Roles.SuperAdmin},{Roles.Admin}")]
    [ProducesResponseType(typeof(ConsultationDetailResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Create([FromBody] CreateConsultationRequest request, CancellationToken ct)
    {
        var result = await _consultations.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.ConsultationId }, result);
    }

    /// <summary>
    /// Update SOAP notes, vitals, or ICD-10 diagnoses. Only allowed while the
    /// consultation is in Draft status.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{Roles.Doctor},{Roles.Nurse},{Roles.SuperAdmin},{Roles.Admin}")]
    [ProducesResponseType(typeof(ConsultationDetailResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateConsultationRequest request, CancellationToken ct)
    {
        var result = await _consultations.UpdateAsync(id, request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Doctor sign-off. Sets status to Signed (immutable after this point) and
    /// auto-completes the linked appointment.
    /// </summary>
    [HttpPost("{id:guid}/sign")]
    [Authorize(Roles = $"{Roles.Doctor},{Roles.SuperAdmin},{Roles.Admin}")]
    [ProducesResponseType(typeof(ConsultationDetailResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Sign(Guid id, CancellationToken ct)
    {
        var result = await _consultations.SignAsync(id, ct);
        return Ok(result);
    }
}
