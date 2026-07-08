using KayCare.Core.DTOs.LabResults;
using KayCare.Core.Exceptions;
using KayCare.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace KayCare.API.Controllers;

[ApiController]
[Route("api/lab-results")]
[Authorize]
public class LabResultsController : ControllerBase
{
    private readonly ILabResultService _lab;
    private readonly IConfiguration _configuration;

    public LabResultsController(ILabResultService lab, IConfiguration configuration)
    {
        _lab = lab;
        _configuration = configuration;
    }

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

    /// <summary>
    /// Secure HTTP webhook for lab analyzer HL7 ORU^R01 messages.
    /// Exposes port 2575 functionality over standard HTTP/HTTPS POST.
    /// </summary>
    [HttpPost("hl7-webhook")]
    [AllowAnonymous]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ReceiveHl7Webhook(CancellationToken ct)
    {
        // 1. Verify Api Key in X-HL7-Key header
        if (!Request.Headers.TryGetValue("X-HL7-Key", out var apiKeyHeader) ||
            string.IsNullOrWhiteSpace(apiKeyHeader.ToString()) ||
            apiKeyHeader.ToString() != _configuration["Hl7:WebhookApiKey"])
        {
            return Unauthorized(new { error = "Invalid or missing HL7 webhook API key." });
        }

        // 2. Read the raw text body
        using var reader = new StreamReader(Request.Body);
        var rawHl7 = await reader.ReadToEndAsync(ct);
        if (string.IsNullOrWhiteSpace(rawHl7))
        {
            return BadRequest(new { error = "HL7 payload is empty." });
        }

        // 3. Process the message
        var success = await _lab.ProcessHl7MessageAsync(rawHl7, ct);
        if (!success)
        {
            return BadRequest(new { error = "Failed to process HL7 message. Verify accession number, patient MRN, and that result does not already exist." });
        }

        return Ok(new { success = true });
    }
}
