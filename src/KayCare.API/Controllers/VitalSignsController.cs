using KayCare.Core.Constants;
using KayCare.Core.DTOs.Nursing;
using KayCare.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KayCare.API.Controllers;

[ApiController]
[Authorize]
public class VitalSignsController : ControllerBase
{
    private readonly IVitalSignsService _vitals;

    public VitalSignsController(IVitalSignsService vitals) => _vitals = vitals;

    [HttpGet("api/patients/{patientId:guid}/vitals")]
    public async Task<IActionResult> GetForPatient(
        Guid patientId, [FromQuery] int limit = 20, CancellationToken ct = default)
        => Ok(await _vitals.GetForPatientAsync(patientId, limit, ct));

    [HttpGet("api/patients/{patientId:guid}/vitals/latest")]
    public async Task<IActionResult> GetLatest(Guid patientId, CancellationToken ct)
    {
        var result = await _vitals.GetLatestAsync(patientId, ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpGet("api/admissions/{admissionId:guid}/vitals")]
    public async Task<IActionResult> GetForAdmission(Guid admissionId, CancellationToken ct)
        => Ok(await _vitals.GetForAdmissionAsync(admissionId, ct));

    [HttpPost("api/patients/{patientId:guid}/vitals")]
    [Authorize(Roles = $"{Roles.Nurse},{Roles.Doctor},{Roles.Admin},{Roles.SuperAdmin}")]
    [ProducesResponseType(typeof(VitalSignsResponse), 201)]
    public async Task<IActionResult> Record(
        Guid patientId, [FromBody] RecordVitalSignsRequest request, CancellationToken ct)
    {
        var result = await _vitals.RecordAsync(patientId, request, ct);
        return CreatedAtAction(nameof(GetLatest), new { patientId }, result);
    }
}
