using KayCare.Core.Constants;
using KayCare.Core.DTOs.Billing;
using KayCare.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KayCare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BillsController : ControllerBase
{
    private readonly IBillingService       _billing;
    private readonly IBillingReportService _report;

    public BillsController(IBillingService billing, IBillingReportService report)
    {
        _billing = billing;
        _report  = report;
    }

    /// <summary>All outstanding bills (Issued or PartiallyPaid) for the tenant.</summary>
    [HttpGet("outstanding")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin},{Roles.Receptionist}")]
    public async Task<IActionResult> GetOutstanding(CancellationToken ct)
    {
        var result = await _billing.GetOutstandingAsync(ct);
        return Ok(result);
    }

    /// <summary>All bills for a specific patient.</summary>
    [HttpGet("patient/{patientId:guid}")]
    public async Task<IActionResult> GetPatientBills(Guid patientId, CancellationToken ct)
    {
        var result = await _billing.GetPatientBillsAsync(patientId, ct);
        return Ok(result);
    }

    /// <summary>Full bill detail including line items and payment history.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BillDetailResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _billing.GetByIdAsync(id, ct);
        return Ok(result);
    }

    /// <summary>Create a new bill in Draft status with one or more line items.</summary>
    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin},{Roles.Receptionist},{Roles.Doctor}")]
    [ProducesResponseType(typeof(BillDetailResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Create([FromBody] CreateBillRequest request, CancellationToken ct)
    {
        var result = await _billing.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.BillId }, result);
    }

    /// <summary>Issue a Draft bill to the patient. Status: Draft → Issued.</summary>
    [HttpPost("{id:guid}/issue")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin},{Roles.Receptionist}")]
    [ProducesResponseType(typeof(BillDetailResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Issue(Guid id, CancellationToken ct)
    {
        var result = await _billing.IssueAsync(id, ct);
        return Ok(result);
    }

    /// <summary>Record a payment against an issued bill. Status advances to PartiallyPaid or Paid.</summary>
    [HttpPost("{id:guid}/payments")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin},{Roles.Receptionist}")]
    [ProducesResponseType(typeof(BillDetailResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> AddPayment(Guid id, [FromBody] AddPaymentRequest request, CancellationToken ct)
    {
        var result = await _billing.AddPaymentAsync(id, request, ct);
        return Ok(result);
    }

    /// <summary>Apply or update a discount/waiver on a Draft or Issued bill.</summary>
    [HttpPatch("{id:guid}/discount")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    [ProducesResponseType(typeof(BillDetailResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ApplyDiscount(Guid id, [FromBody] ApplyDiscountRequest request, CancellationToken ct)
    {
        var result = await _billing.ApplyDiscountAsync(id, request, ct);
        return Ok(result);
    }

    /// <summary>Cancel a Draft or Issued bill. Status: Draft|Issued → Cancelled.</summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    [ProducesResponseType(typeof(BillDetailResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var result = await _billing.CancelAsync(id, ct);
        return Ok(result);
    }

    /// <summary>Void a Paid or PartiallyPaid bill. Status: Paid|PartiallyPaid → Void.</summary>
    [HttpPost("{id:guid}/void")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    [ProducesResponseType(typeof(BillDetailResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Void(Guid id, CancellationToken ct)
    {
        var result = await _billing.VoidAsync(id, ct);
        return Ok(result);
    }

    /// <summary>Download a PDF invoice for the bill.</summary>
    [HttpGet("{id:guid}/report")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetInvoice(Guid id, CancellationToken ct)
    {
        var pdf = await _report.GenerateBillInvoiceAsync(id, ct);
        if (pdf == null) return NotFound();
        return File(pdf, "application/pdf", $"Invoice-{id.ToString("N")[..8]}.pdf");
    }

    /// <summary>Download a PDF payment receipt.</summary>
    [HttpGet("payments/{paymentId:guid}/receipt")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetReceipt(Guid paymentId, CancellationToken ct)
    {
        var pdf = await _report.GeneratePaymentReceiptAsync(paymentId, ct);
        if (pdf == null) return NotFound();
        return File(pdf, "application/pdf", $"Receipt-{paymentId.ToString("N")[..8]}.pdf");
    }
}
