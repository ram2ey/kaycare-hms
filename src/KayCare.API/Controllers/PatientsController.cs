using KayCare.Core.Constants;
using KayCare.Core.DTOs.Patients;
using KayCare.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KayCare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _patients;

    public PatientsController(IPatientService patients)
    {
        _patients = patients;
    }

    /// <summary>Search patients by name, MRN, or phone number.</summary>
    [HttpGet]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Search([FromQuery] PatientSearchRequest request, CancellationToken ct)
    {
        var result = await _patients.SearchAsync(request, ct);
        return Ok(result);
    }

    /// <summary>Get full patient record by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var patient = await _patients.GetByIdAsync(id, ct);
        return Ok(patient);
    }

    /// <summary>Register a new patient. Generates MRN automatically.</summary>
    [HttpPost]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Doctor},{Roles.Nurse},{Roles.Receptionist}")]
    [ProducesResponseType(typeof(PatientDetailResponse), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Register([FromBody] CreatePatientRequest request, CancellationToken ct)
    {
        var patient = await _patients.RegisterAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = patient.PatientId }, patient);
    }

    /// <summary>Update patient demographic and contact information.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Doctor},{Roles.Nurse},{Roles.Receptionist}")]
    [ProducesResponseType(typeof(PatientDetailResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePatientRequest request, CancellationToken ct)
    {
        var patient = await _patients.UpdateAsync(id, request, ct);
        return Ok(patient);
    }

    // ── Allergies ─────────────────────────────────────────────────────────────

    /// <summary>List all allergies for a patient.</summary>
    [HttpGet("{id:guid}/allergies")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetAllergies(Guid id, CancellationToken ct)
    {
        var allergies = await _patients.GetAllergiesAsync(id, ct);
        return Ok(allergies);
    }

    /// <summary>Add an allergy to a patient record.</summary>
    [HttpPost("{id:guid}/allergies")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Doctor},{Roles.Nurse}")]
    [ProducesResponseType(typeof(AllergyResponse), 201)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> AddAllergy(Guid id, [FromBody] AddAllergyRequest request, CancellationToken ct)
    {
        var allergy = await _patients.AddAllergyAsync(id, request, ct);
        return CreatedAtAction(nameof(GetAllergies), new { id }, allergy);
    }

    /// <summary>Remove an allergy from a patient record.</summary>
    [HttpDelete("{id:guid}/allergies/{allergyId:guid}")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Doctor}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> RemoveAllergy(Guid id, Guid allergyId, CancellationToken ct)
    {
        await _patients.RemoveAllergyAsync(id, allergyId, ct);
        return NoContent();
    }
}
