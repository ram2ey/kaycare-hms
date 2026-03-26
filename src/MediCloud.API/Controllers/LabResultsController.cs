using MediCloud.Core.DTOs.LabResults;
using MediCloud.Core.Exceptions;
using MediCloud.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediCloud.API.Controllers;

[ApiController]
[Route("api/lab-results")]
[Authorize]
public class LabResultsController : ControllerBase
{
    private readonly ILabResultService _lab;

    public LabResultsController(ILabResultService lab) => _lab = lab;

    /// <summary>All lab results for a patient, newest first.</summary>
    [HttpGet("patient/{patientId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<LabResultResponse>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetByPatient(Guid patientId, CancellationToken ct)
    {
        var results = await _lab.GetByPatientAsync(patientId, ct);
        return Ok(results);
    }

    /// <summary>
    /// Get a lab result by accession number (includes all OBX observations).
    /// </summary>
    [HttpGet("order/{accessionNumber}")]
    [ProducesResponseType(typeof(LabResultDetailResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetByAccession(string accessionNumber, CancellationToken ct)
    {
        var result = await _lab.GetByAccessionAsync(accessionNumber, ct);
        if (result == null) throw new NotFoundException("LabResult", accessionNumber);
        return Ok(result);
    }

    /// <summary>
    /// Get a lab result by ID (includes all OBX observations).
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LabResultDetailResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _lab.GetByIdAsync(id, ct);
        if (result == null) throw new NotFoundException("LabResult", id);
        return Ok(result);
    }
}
