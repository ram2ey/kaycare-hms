namespace KayCare.Core.DTOs.Documents;

public class DocumentResponse
{
    public Guid     DocumentId      { get; set; }
    public Guid     PatientId       { get; set; }
    public string   PatientName     { get; set; } = string.Empty;
    public Guid?    ConsultationId  { get; set; }
    public string   FileName        { get; set; } = string.Empty;
    public string   ContentType     { get; set; } = string.Empty;
    public long     FileSizeBytes   { get; set; }
    public string   Category        { get; set; } = string.Empty;
    public string?  Description     { get; set; }
    public string   UploadedByName  { get; set; } = string.Empty;
    public DateTime CreatedAt       { get; set; }
}
