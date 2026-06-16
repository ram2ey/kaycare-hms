using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using KayCare.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KayCare.API.Controllers;

[ApiController]
[Route("api/dev/hl7")]
[AllowAnonymous]
public class DevController : ControllerBase
{
    private readonly ILabResultService _labResultService;

    public DevController(ILabResultService labResultService)
    {
        _labResultService = labResultService;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendSimulatedResults([FromBody] DevHl7SendRequest request, CancellationToken ct)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.AccessionNumber))
        {
            return BadRequest(new { error = "AccessionNumber is required." });
        }

        var ts = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var sb = new StringBuilder();
        sb.Append($"MSH|^~\\&|LIMS|FACILITY|ANALYZER|FACILITY|{ts}||ORU^R01|MSG{ts[8..]}||2.3\r");
        sb.Append($"PID|||{request.PatientMrn}||{request.PatientName.ToUpper().Replace(" ", "^")}||19850615|M\r");
        sb.Append($"OBR|||{request.AccessionNumber}|^DIAGNOSTIC_PANEL|||{ts}|||||||||{request.DoctorId}\r");

        for (int i = 0; i < request.Observations.Count; i++)
        {
            var obs = request.Observations[i];
            var flag = string.IsNullOrWhiteSpace(obs.AbnormalFlag) || obs.AbnormalFlag == "N" ? "" : obs.AbnormalFlag;
            sb.Append($"OBX|{i + 1}|NM|{obs.TestCode}^{obs.TestName}^LN||{obs.Value}|{obs.Unit}|^{obs.ReferenceRange}|{flag}|||F\r");
        }

        var rawHl7 = sb.ToString();

        var success = await _labResultService.ProcessHl7MessageAsync(rawHl7, ct);
        if (!success)
        {
            return BadRequest(new { error = "Failed to process simulated HL7 message. Check accession number mapping and ensure no duplicate result exists." });
        }

        return Ok(new { success = true, rawHl7Message = rawHl7 });
    }
}

public class DevHl7SendRequest
{
    public string AccessionNumber { get; set; } = string.Empty;
    public string PatientMrn { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public List<DevHl7Observation> Observations { get; set; } = [];
}

public class DevHl7Observation
{
    public string TestCode { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public string? ReferenceRange { get; set; }
    public string? AbnormalFlag { get; set; }
}
