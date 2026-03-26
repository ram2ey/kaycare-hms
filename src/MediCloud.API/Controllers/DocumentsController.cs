using MediCloud.Core.DTOs.Documents;
using MediCloud.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediCloud.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documents;

    public DocumentsController(IDocumentService documents)
    {
        _documents = documents;
    }

    /// <summary>All documents for a specific patient, newest first.</summary>
    [HttpGet("patient/{patientId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentResponse>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetByPatient(Guid patientId, CancellationToken ct)
    {
        var result = await _documents.GetByPatientAsync(patientId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns a 15-minute SAS download URL for a single document.
    /// Redirect the browser or client directly to the returned URL.
    /// </summary>
    [HttpGet("{id:guid}/download-url")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetDownloadUrl(Guid id, CancellationToken ct)
    {
        var url = await _documents.GetDownloadUrlAsync(id, ct);
        return Ok(new { downloadUrl = url, expiresInMinutes = 15 });
    }

    /// <summary>
    /// Upload a document for a patient. Send as multipart/form-data.
    /// Fields: patientId (guid), consultationId (guid, optional), category (string), description (string, optional), file (binary).
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(DocumentResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Upload(
        [FromForm] Guid    patientId,
        [FromForm] Guid?   consultationId,
        [FromForm] string  category,
        [FromForm] string? description,
        IFormFile          file,
        CancellationToken  ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "A non-empty file is required." });

        var request = new UploadDocumentRequest
        {
            PatientId      = patientId,
            ConsultationId = consultationId,
            Category       = category ?? "Other",
            Description    = description
        };

        var uploadInfo = new FileUploadInfo(
            file.OpenReadStream(),
            file.FileName,
            file.ContentType,
            file.Length);

        var result = await _documents.UploadAsync(request, uploadInfo, ct);
        return CreatedAtAction(nameof(GetDownloadUrl), new { id = result.DocumentId }, result);
    }

    /// <summary>Permanently deletes a document from blob storage and the database.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _documents.DeleteAsync(id, ct);
        return NoContent();
    }
}
