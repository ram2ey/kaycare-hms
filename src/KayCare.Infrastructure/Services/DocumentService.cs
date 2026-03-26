using KayCare.Core.DTOs.Documents;
using KayCare.Core.Entities;
using KayCare.Core.Exceptions;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KayCare.Infrastructure.Services;

public class DocumentService : IDocumentService
{
    private static readonly TimeSpan SasExpiry = TimeSpan.FromMinutes(15);

    private readonly AppDbContext        _db;
    private readonly IBlobStorageService _blob;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext      _tenantContext;

    public DocumentService(
        AppDbContext        db,
        IBlobStorageService blob,
        ICurrentUserService currentUser,
        ITenantContext      tenantContext)
    {
        _db            = db;
        _blob          = blob;
        _currentUser   = currentUser;
        _tenantContext = tenantContext;
    }

    // ── Upload ────────────────────────────────────────────────────────────────

    public async Task<DocumentResponse> UploadAsync(
        UploadDocumentRequest request,
        FileUploadInfo        file,
        CancellationToken     ct = default)
    {
        var patientExists = await _db.Patients.AnyAsync(p => p.PatientId == request.PatientId, ct);
        if (!patientExists) throw new NotFoundException(nameof(Patient), request.PatientId);

        // Sanitize the tenant code to produce a valid Azure container name:
        // lowercase letters, numbers, and hyphens; 3–63 chars.
        var containerName = BuildContainerName(_tenantContext.TenantCode);

        // Unique blob path so re-uploads never collide:  patients/{patientId}/{newGuid}/{filename}
        var documentId = Guid.NewGuid();
        var safeFilename = SanitizeFilename(file.FileName);
        var blobPath = $"patients/{request.PatientId}/{documentId}/{safeFilename}";

        await _blob.UploadAsync(containerName, blobPath, file.Content, file.ContentType, ct);

        var document = new PatientDocument
        {
            DocumentId       = documentId,
            PatientId        = request.PatientId,
            ConsultationId   = request.ConsultationId,
            UploadedByUserId = _currentUser.UserId,
            FileName         = file.FileName,
            ContentType      = file.ContentType,
            FileSizeBytes    = file.SizeBytes,
            Category         = request.Category,
            Description      = request.Description,
            BlobPath         = blobPath,
            ContainerName    = containerName
        };

        _db.PatientDocuments.Add(document);
        await _db.SaveChangesAsync(ct);

        return await LoadResponseAsync(document.DocumentId, ct);
    }

    // ── Read ──────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<DocumentResponse>> GetByPatientAsync(Guid patientId, CancellationToken ct = default)
    {
        var patientExists = await _db.Patients.AnyAsync(p => p.PatientId == patientId, ct);
        if (!patientExists) throw new NotFoundException(nameof(Patient), patientId);

        var rows = await _db.PatientDocuments
            .Include(d => d.Patient)
            .Include(d => d.UploadedBy)
            .AsNoTracking()
            .Where(d => d.PatientId == patientId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);

        return rows.Select(MapToResponse).ToList();
    }

    // ── Download (SAS URL) ────────────────────────────────────────────────────

    public async Task<string> GetDownloadUrlAsync(Guid documentId, CancellationToken ct = default)
    {
        var doc = await _db.PatientDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DocumentId == documentId, ct)
            ?? throw new NotFoundException(nameof(PatientDocument), documentId);

        var uri = _blob.GenerateSasUri(doc.ContainerName, doc.BlobPath, SasExpiry);
        return uri.ToString();
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    public async Task DeleteAsync(Guid documentId, CancellationToken ct = default)
    {
        var doc = await _db.PatientDocuments
            .FirstOrDefaultAsync(d => d.DocumentId == documentId, ct)
            ?? throw new NotFoundException(nameof(PatientDocument), documentId);

        // Remove from blob storage first; if this fails the DB record is preserved
        await _blob.DeleteAsync(doc.ContainerName, doc.BlobPath, ct);

        _db.PatientDocuments.Remove(doc);
        await _db.SaveChangesAsync(ct);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<DocumentResponse> LoadResponseAsync(Guid documentId, CancellationToken ct)
    {
        var doc = await _db.PatientDocuments
            .Include(d => d.Patient)
            .Include(d => d.UploadedBy)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DocumentId == documentId, ct)
            ?? throw new NotFoundException(nameof(PatientDocument), documentId);

        return MapToResponse(doc);
    }

    private static DocumentResponse MapToResponse(PatientDocument d) => new()
    {
        DocumentId     = d.DocumentId,
        PatientId      = d.PatientId,
        PatientName    = $"{d.Patient.FirstName} {d.Patient.LastName}".Trim(),
        ConsultationId = d.ConsultationId,
        FileName       = d.FileName,
        ContentType    = d.ContentType,
        FileSizeBytes  = d.FileSizeBytes,
        Category       = d.Category,
        Description    = d.Description,
        UploadedByName = $"{d.UploadedBy.FirstName} {d.UploadedBy.LastName}".Trim(),
        CreatedAt      = d.CreatedAt
    };

    /// <summary>
    /// Builds a valid Azure Blob container name from the tenant code.
    /// Rules: 3–63 chars, lowercase, letters/digits/hyphens only, no consecutive hyphens.
    /// </summary>
    private static string BuildContainerName(string tenantCode)
    {
        var sanitized = new string(
            tenantCode.ToLower()
                      .Select(c => char.IsLetterOrDigit(c) ? c : '-')
                      .ToArray());

        // Collapse consecutive hyphens and trim leading/trailing hyphens
        while (sanitized.Contains("--"))
            sanitized = sanitized.Replace("--", "-");
        sanitized = sanitized.Trim('-');

        var name = $"tenant-{sanitized}";

        // Enforce 63-char max
        if (name.Length > 63)
            name = name[..63].TrimEnd('-');

        return name;
    }

    /// <summary>Strips path separators and other unsafe characters from a filename.</summary>
    private static string SanitizeFilename(string filename)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(filename.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }
}
