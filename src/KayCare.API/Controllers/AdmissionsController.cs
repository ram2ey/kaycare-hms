using KayCare.Core.Constants;
using KayCare.Core.DTOs.Inpatient;
using KayCare.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KayCare.API.Controllers;

[ApiController]
[Route("api/admissions")]
[Authorize]
public class AdmissionsController : ControllerBase
{
    private readonly IAdmissionService              _admissions;
    private readonly IInpatientBillingService       _billing;
    private readonly IDischargeSummaryReportService _summaryReport;

    public AdmissionsController(
        IAdmissionService admissions,
        IInpatientBillingService billing,
        IDischargeSummaryReportService summaryReport)
    {
        _admissions    = admissions;
        _billing       = billing;
        _summaryReport = summaryReport;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status    = null,
        [FromQuery] Guid?   wardId    = null,
        [FromQuery] Guid?   patientId = null,
        CancellationToken ct = default)
        => Ok(await _admissions.GetAllAsync(status, wardId, patientId, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _admissions.GetByIdAsync(id, ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpGet("patient/{patientId:guid}")]
    public async Task<IActionResult> GetPatientHistory(Guid patientId, CancellationToken ct)
        => Ok(await _admissions.GetPatientHistoryAsync(patientId, ct));

    [HttpPost]
    [Authorize(Roles = $"{Roles.Doctor},{Roles.Admin},{Roles.SuperAdmin},{Roles.Receptionist}")]
    [ProducesResponseType(typeof(AdmissionDetailResponse), 201)]
    public async Task<IActionResult> Admit([FromBody] AdmitPatientRequest request, CancellationToken ct)
    {
        var result = await _admissions.AdmitAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.AdmissionId }, result);
    }

    [HttpPost("{id:guid}/discharge")]
    [Authorize(Roles = $"{Roles.Doctor},{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<IActionResult> Discharge(Guid id, [FromBody] DischargePatientRequest request, CancellationToken ct)
        => Ok(await _admissions.DischargeAsync(id, request, ct));

    [HttpPost("{id:guid}/transfer")]
    [Authorize(Roles = $"{Roles.Doctor},{Roles.Nurse},{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<IActionResult> Transfer(Guid id, [FromBody] TransferPatientRequest request, CancellationToken ct)
        => Ok(await _admissions.TransferAsync(id, request, ct));

    // ── Discharge Summary ─────────────────────────────────────────────────────

    [HttpGet("{id:guid}/discharge-summary")]
    public async Task<IActionResult> GetDischargeSummary(Guid id, CancellationToken ct)
        => Ok(await _admissions.GetDischargeSummaryAsync(id, ct));

    [HttpPut("{id:guid}/discharge-summary")]
    [Authorize(Roles = $"{Roles.Doctor},{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<IActionResult> UpdateDischargeSummary(
        Guid id, [FromBody] UpdateDischargeSummaryRequest request, CancellationToken ct)
        => Ok(await _admissions.UpdateDischargeSummaryAsync(id, request, ct));

    [HttpGet("{id:guid}/discharge-summary/report")]
    public async Task<IActionResult> GetDischargeSummaryReport(Guid id, CancellationToken ct)
    {
        var summary = await _admissions.GetDischargeSummaryAsync(id, ct);
        var pdf     = await _summaryReport.GenerateAsync(summary, ct);
        return File(pdf, "application/pdf", $"DischargeSummary-{summary.AdmissionNumber}.pdf");
    }

    // ── Inpatient Billing ─────────────────────────────────────────────────────

    [HttpGet("{id:guid}/charges")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin},{Roles.Doctor},{Roles.Nurse},{Roles.Receptionist}")]
    public async Task<IActionResult> GetCharges(Guid id, CancellationToken ct)
        => Ok(await _billing.GetChargesAsync(id, ct));

    [HttpGet("{id:guid}/billing-summary")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin},{Roles.Doctor},{Roles.Nurse},{Roles.Receptionist}")]
    public async Task<IActionResult> GetBillingSummary(Guid id, CancellationToken ct)
        => Ok(await _billing.GetBillSummaryAsync(id, ct));

    [HttpPost("{id:guid}/charges")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin},{Roles.Receptionist}")]
    public async Task<IActionResult> AddCharge(Guid id, [FromBody] SaveInpatientChargeRequest request, CancellationToken ct)
        => StatusCode(201, await _billing.AddChargeAsync(id, request, ct));

    [HttpPut("{id:guid}/charges/{chargeId:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin},{Roles.Receptionist}")]
    public async Task<IActionResult> UpdateCharge(Guid id, Guid chargeId, [FromBody] SaveInpatientChargeRequest request, CancellationToken ct)
        => Ok(await _billing.UpdateChargeAsync(chargeId, request, ct));

    [HttpDelete("{id:guid}/charges/{chargeId:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin},{Roles.Receptionist}")]
    public async Task<IActionResult> RemoveCharge(Guid id, Guid chargeId, CancellationToken ct)
    {
        await _billing.RemoveChargeAsync(chargeId, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/charges/apply-accommodation")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin},{Roles.Receptionist}")]
    public async Task<IActionResult> ApplyAccommodation(Guid id, CancellationToken ct)
        => Ok(await _billing.ApplyAccommodationChargesAsync(id, ct));

    [HttpPost("{id:guid}/generate-bill")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin},{Roles.Receptionist}")]
    public async Task<IActionResult> GenerateBill(Guid id, CancellationToken ct)
    {
        var billId = await _billing.GenerateBillAsync(id, ct);
        return Ok(new { billId });
    }
}
