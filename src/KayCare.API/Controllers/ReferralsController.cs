using KayCare.Core.Constants;
using KayCare.Core.DTOs.Referrals;
using KayCare.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KayCare.API.Controllers;

[ApiController]
[Route("api/referrals")]
[Authorize]
public class ReferralsController : ControllerBase
{
    private readonly IReferralService _referrals;

    public ReferralsController(IReferralService referrals) => _referrals = referrals;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status, CancellationToken ct)
        => Ok(await _referrals.GetAllAsync(status, ct));

    [HttpGet("incoming")]
    [Authorize(Roles = $"{Roles.Doctor},{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<IActionResult> GetIncoming(CancellationToken ct)
        => Ok(await _referrals.GetIncomingAsync(ct));

    [HttpGet("patient/{patientId:guid}")]
    public async Task<IActionResult> GetForPatient(Guid patientId, CancellationToken ct)
        => Ok(await _referrals.GetForPatientAsync(patientId, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await _referrals.GetByIdAsync(id, ct));

    [HttpPost]
    [Authorize(Roles = $"{Roles.Doctor},{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<IActionResult> Create([FromBody] CreateReferralRequest req, CancellationToken ct)
    {
        var result = await _referrals.CreateAsync(req, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.ReferralId }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{Roles.Doctor},{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateReferralRequest req, CancellationToken ct)
        => Ok(await _referrals.UpdateAsync(id, req, ct));

    [HttpPost("{id:guid}/send")]
    [Authorize(Roles = $"{Roles.Doctor},{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<IActionResult> Send(Guid id, CancellationToken ct)
        => Ok(await _referrals.SendAsync(id, ct));

    [HttpPost("{id:guid}/respond")]
    [Authorize(Roles = $"{Roles.Doctor},{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<IActionResult> Respond(Guid id, [FromBody] RespondToReferralRequest req, CancellationToken ct)
        => Ok(await _referrals.RespondAsync(id, req, ct));

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = $"{Roles.Doctor},{Roles.Admin},{Roles.SuperAdmin},{Roles.Receptionist}")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
        => Ok(await _referrals.CancelAsync(id, ct));
}
