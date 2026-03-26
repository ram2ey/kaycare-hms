using MediCloud.Core.Constants;
using MediCloud.Core.DTOs.Appointments;
using MediCloud.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediCloud.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointments;

    public AppointmentsController(IAppointmentService appointments)
    {
        _appointments = appointments;
    }

    /// <summary>Doctor's calendar view. Defaults to the next 7 days.</summary>
    [HttpGet("calendar")]
    public async Task<IActionResult> GetCalendar([FromQuery] CalendarRequest request, CancellationToken ct)
    {
        var result = await _appointments.GetCalendarAsync(request, ct);
        return Ok(result);
    }

    /// <summary>All appointments for a specific patient.</summary>
    [HttpGet("patient/{patientId:guid}")]
    public async Task<IActionResult> GetPatientAppointments(Guid patientId, CancellationToken ct)
    {
        var result = await _appointments.GetPatientAppointmentsAsync(patientId, ct);
        return Ok(result);
    }

    /// <summary>Get a single appointment by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AppointmentDetailResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _appointments.GetByIdAsync(id, ct);
        return Ok(result);
    }

    /// <summary>Schedule a new appointment. Checks doctor availability automatically.</summary>
    [HttpPost]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Doctor},{Roles.Nurse},{Roles.Receptionist}")]
    [ProducesResponseType(typeof(AppointmentDetailResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentRequest request, CancellationToken ct)
    {
        var result = await _appointments.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.AppointmentId }, result);
    }

    /// <summary>Update appointment details (time, doctor, notes). Availability is re-checked if time or doctor changes.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Doctor},{Roles.Nurse},{Roles.Receptionist}")]
    [ProducesResponseType(typeof(AppointmentDetailResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAppointmentRequest request, CancellationToken ct)
    {
        var result = await _appointments.UpdateAsync(id, request, ct);
        return Ok(result);
    }

    // ── Status transition endpoints ───────────────────────────────────────────

    /// <summary>Confirm a scheduled appointment.</summary>
    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct)
    {
        var result = await _appointments.TransitionStatusAsync(id, AppointmentStatus.Confirmed, ct: ct);
        return Ok(result);
    }

    /// <summary>Check the patient in on arrival.</summary>
    [HttpPost("{id:guid}/check-in")]
    public async Task<IActionResult> CheckIn(Guid id, CancellationToken ct)
    {
        var result = await _appointments.TransitionStatusAsync(id, AppointmentStatus.CheckedIn, ct: ct);
        return Ok(result);
    }

    /// <summary>Mark appointment as in progress (doctor has started).</summary>
    [HttpPost("{id:guid}/start")]
    [Authorize(Roles = $"{Roles.Doctor},{Roles.SuperAdmin},{Roles.Admin}")]
    public async Task<IActionResult> Start(Guid id, CancellationToken ct)
    {
        var result = await _appointments.TransitionStatusAsync(id, AppointmentStatus.InProgress, ct: ct);
        return Ok(result);
    }

    /// <summary>Complete an in-progress appointment.</summary>
    [HttpPost("{id:guid}/complete")]
    [Authorize(Roles = $"{Roles.Doctor},{Roles.SuperAdmin},{Roles.Admin}")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        var result = await _appointments.TransitionStatusAsync(id, AppointmentStatus.Completed, ct: ct);
        return Ok(result);
    }

    /// <summary>Cancel an appointment with an optional reason.</summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelAppointmentRequest request, CancellationToken ct)
    {
        var result = await _appointments.TransitionStatusAsync(id, AppointmentStatus.Cancelled, request.Reason, ct);
        return Ok(result);
    }

    /// <summary>Mark a patient as a no-show.</summary>
    [HttpPost("{id:guid}/no-show")]
    public async Task<IActionResult> NoShow(Guid id, CancellationToken ct)
    {
        var result = await _appointments.TransitionStatusAsync(id, AppointmentStatus.NoShow, ct: ct);
        return Ok(result);
    }
}
