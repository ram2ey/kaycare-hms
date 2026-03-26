using KayCare.Core.DTOs.LabOrders;
using KayCare.Core.Exceptions;
using KayCare.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace KayCare.API.Controllers;

[ApiController]
[Route("api/lab-orders")]
[Authorize]
public class LabOrdersController : ControllerBase
{
    private readonly ILabOrderService  _labOrders;
    private readonly ILabReportService _labReport;

    public LabOrdersController(ILabOrderService labOrders, ILabReportService labReport)
    {
        _labOrders  = labOrders;
        _labReport  = labReport;
    }

    /// <summary>All active tests in the catalog (for order form dropdowns).</summary>
    [HttpGet("catalog")]
    [ProducesResponseType(typeof(IReadOnlyList<LabTestCatalogResponse>), 200)]
    public async Task<IActionResult> GetCatalog(CancellationToken ct)
        => Ok(await _labOrders.GetTestCatalogAsync(ct));

    /// <summary>
    /// Waiting list — orders for a given date.
    /// Mirrors the CrelioHealth waiting list view.
    /// </summary>
    [HttpGet("waiting-list")]
    [ProducesResponseType(typeof(IReadOnlyList<LabOrderResponse>), 200)]
    public async Task<IActionResult> GetWaitingList(
        [FromQuery] DateOnly? date,
        [FromQuery] string?   status,
        [FromQuery] string?   department,
        CancellationToken ct)
    {
        var d = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return Ok(await _labOrders.GetWaitingListAsync(d, status, department, ct));
    }

    /// <summary>All lab orders for a patient, newest first.</summary>
    [HttpGet("patient/{patientId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<LabOrderResponse>), 200)]
    public async Task<IActionResult> GetByPatient(Guid patientId, CancellationToken ct)
        => Ok(await _labOrders.GetByPatientAsync(patientId, ct));

    /// <summary>Lab order detail with all items.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LabOrderDetailResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var order = await _labOrders.GetByIdAsync(id, ct);
        if (order == null) throw new NotFoundException("LabOrder", id);
        return Ok(order);
    }

    /// <summary>Doctor places a new lab order.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(LabOrderDetailResponse), 201)]
    public async Task<IActionResult> PlaceOrder(
        [FromBody] CreateLabOrderRequest req, CancellationToken ct)
    {
        var order = await _labOrders.PlaceOrderAsync(req, ct);
        return CreatedAtAction(nameof(GetById), new { id = order.LabOrderId }, order);
    }

    /// <summary>
    /// Phlebotomist marks a sample as received.
    /// Generates an accession number — print this as a barcode on the sample tube.
    /// </summary>
    [HttpPatch("items/{itemId:guid}/receive")]
    [ProducesResponseType(typeof(LabOrderItemResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> ReceiveSample(Guid itemId, CancellationToken ct)
    {
        var item = await _labOrders.ReceiveSampleAsync(itemId, ct);
        return Ok(item);
    }

    /// <summary>
    /// Lab technician enters a manual result (malaria, WIDAL, urinalysis, etc.).
    /// Only valid for items where IsManualEntry = true.
    /// </summary>
    [HttpPost("items/{itemId:guid}/result")]
    [ProducesResponseType(typeof(LabOrderItemResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> EnterManualResult(
        Guid itemId,
        [FromBody] ManualResultRequest req,
        CancellationToken ct)
    {
        var item = await _labOrders.EnterManualResultAsync(itemId, req, ct);
        return Ok(item);
    }

    /// <summary>
    /// Downloads a PDF lab result report for the given order.
    /// Includes all resulted tests with reference ranges and abnormal flags.
    /// </summary>
    [HttpGet("{id:guid}/report")]
    [Produces(MediaTypeNames.Application.Pdf)]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DownloadReport(Guid id, CancellationToken ct)
    {
        var pdf = await _labReport.GenerateLabOrderReportAsync(id, ct);
        if (pdf == null) throw new NotFoundException("LabOrder", id);

        var order = await _labOrders.GetByIdAsync(id, ct);
        var filename = $"LabReport-{order!.PatientMrn}-{DateTime.Now:yyyyMMdd}.pdf";
        return File(pdf, MediaTypeNames.Application.Pdf, filename);
    }

    /// <summary>Doctor or lab manager signs off on a resulted item.</summary>
    [HttpPatch("items/{itemId:guid}/sign")]
    [ProducesResponseType(typeof(LabOrderItemResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> SignItem(Guid itemId, CancellationToken ct)
    {
        var item = await _labOrders.SignItemAsync(itemId, ct);
        return Ok(item);
    }
}
