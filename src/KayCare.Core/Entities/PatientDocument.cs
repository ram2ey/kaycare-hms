namespace KayCare.Core.Entities;

public class PatientDocument : TenantEntity
{
    public Guid    DocumentId       { get; set; }
    public Guid    PatientId        { get; set; }
    public Guid?   ConsultationId   { get; set; }
    public Guid    UploadedByUserId { get; set; }
    public string  FileName         { get; set; } = string.Empty;   // original filename shown to users
    public string  ContentType      { get; set; } = string.Empty;   // MIME type
    public long    FileSizeBytes    { get; set; }
    public string  Category         { get; set; } = "Other";        // LabResult | Prescription | Referral | Consent | Report | Other
    public string? Description      { get; set; }
    public string  BlobPath         { get; set; } = string.Empty;   // path within the tenant container
    public string  ContainerName    { get; set; } = string.Empty;   // per-tenant blob container

    public Patient Patient    { get; set; } = null!;
    public User    UploadedBy { get; set; } = null!;
}
