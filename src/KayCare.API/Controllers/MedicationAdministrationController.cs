using KayCare.Core.Constants;
using KayCare.Core.DTOs.Nursing;
using KayCare.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KayCare.API.Controllers;

[ApiController]
[Route("api/mar")]
[Authorize]
public class MedicationAdministrationController : ControllerBase
{
    private readonly IMedicationAdministrationService _mar;

    public MedicationAdministrationController(IMedicationAdministrationService mar) => _mar = mar;

    [HttpGet("patient/{patientId:guid}")]
    public async Task<IActionResult> GetForPatient(Guid patientId, CancellationToken ct)
        => Ok(await _mar.GetMARForPatientAsync(patientId, ct));

    [HttpGet("admission/{admissionId:guid}")]
    public async Task<IActionResult> GetForAdmission(Guid admissionId, CancellationToken ct)
        => Ok(await _mar.GetMARForAdmissionAsync(admissionId, ct));

    [HttpPost]
    [Authorize(Roles = $"{Roles.Nurse},{Roles.Admin},{Roles.SuperAdmin}")]
    [ProducesResponseType(typeof(MAREntryResponse), 201)]
    public async Task<IActionResult> Record([FromBody] RecordAdministrationRequest request, CancellationToken ct)
    {
        var result = await _mar.RecordAsync(request, ct);
        return StatusCode(201, result);
    }
}
