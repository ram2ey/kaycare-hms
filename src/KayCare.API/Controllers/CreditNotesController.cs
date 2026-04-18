using KayCare.Core.Constants;
using KayCare.Core.DTOs.Billing;
using KayCare.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KayCare.API.Controllers;

[ApiController]
[Route("api/credit-notes")]
[Authorize]
public class CreditNotesController : ControllerBase
{
    private readonly ICreditNoteService _creditNotes;

    public CreditNotesController(ICreditNoteService creditNotes)
    {
        _creditNotes = creditNotes;
    }

    /// <summary>List credit notes with optional filters: status, billId, patientId.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] Guid?   billId,
        [FromQuery] Guid?   patientId,
        CancellationToken   ct)
    {
        var result = await _creditNotes.GetAllAsync(status, billId, patientId, ct);
        return Ok(result);
    }

    /// <summary>Get a single credit note by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _creditNotes.GetByIdAsync(id, ct);
        return result == null ? NotFound() : Ok(result);
    }

    /// <summary>Create a new credit note (Draft).</summary>
    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin},{Roles.BillingOfficer}")]
    [ProducesResponseType(typeof(CreditNoteResponse), 201)]
    public async Task<IActionResult> Create([FromBody] CreateCreditNoteRequest request, CancellationToken ct)
    {
        var result = await _creditNotes.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.CreditNoteId }, result);
    }

    /// <summary>Approve a Draft credit note.</summary>
    [HttpPut("{id:guid}/approve")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    [ProducesResponseType(typeof(CreditNoteResponse), 200)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        var result = await _creditNotes.ApproveAsync(id, ct);
        return Ok(result);
    }

    /// <summary>Apply an Approved credit note to its bill (reduces balance).</summary>
    [HttpPut("{id:guid}/apply")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    [ProducesResponseType(typeof(CreditNoteResponse), 200)]
    public async Task<IActionResult> Apply(Guid id, CancellationToken ct)
    {
        var result = await _creditNotes.ApplyAsync(id, ct);
        return Ok(result);
    }

    /// <summary>Void a Draft or Approved credit note.</summary>
    [HttpPut("{id:guid}/void")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    [ProducesResponseType(typeof(CreditNoteResponse), 200)]
    public async Task<IActionResult> Void(Guid id, CancellationToken ct)
    {
        var result = await _creditNotes.VoidAsync(id, ct);
        return Ok(result);
    }
}
