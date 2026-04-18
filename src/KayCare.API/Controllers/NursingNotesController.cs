using KayCare.Core.Constants;
using KayCare.Core.DTOs.Nursing;
using KayCare.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KayCare.API.Controllers;

[ApiController]
[Authorize]
public class NursingNotesController : ControllerBase
{
    private readonly INursingNoteService _notes;

    public NursingNotesController(INursingNoteService notes) => _notes = notes;

    [HttpGet("api/patients/{patientId:guid}/nursing-notes")]
    public async Task<IActionResult> GetForPatient(Guid patientId, CancellationToken ct)
        => Ok(await _notes.GetForPatientAsync(patientId, ct));

    [HttpGet("api/admissions/{admissionId:guid}/nursing-notes")]
    public async Task<IActionResult> GetForAdmission(Guid admissionId, CancellationToken ct)
        => Ok(await _notes.GetForAdmissionAsync(admissionId, ct));

    [HttpPost("api/patients/{patientId:guid}/nursing-notes")]
    [Authorize(Roles = $"{Roles.Nurse},{Roles.Doctor},{Roles.Admin},{Roles.SuperAdmin}")]
    [ProducesResponseType(typeof(NursingNoteResponse), 201)]
    public async Task<IActionResult> Add(
        Guid patientId, [FromBody] AddNursingNoteRequest request, CancellationToken ct)
    {
        var result = await _notes.AddAsync(patientId, request, ct);
        return CreatedAtAction(nameof(GetForPatient), new { patientId }, result);
    }

    [HttpDelete("api/nursing-notes/{id:guid}")]
    [Authorize(Roles = $"{Roles.Nurse},{Roles.Doctor},{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _notes.DeleteAsync(id, ct);
        return NoContent();
    }
}
