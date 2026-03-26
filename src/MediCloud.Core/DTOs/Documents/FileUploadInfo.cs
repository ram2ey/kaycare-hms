namespace MediCloud.Core.DTOs.Documents;

/// <summary>Carries the raw file bytes alongside metadata; keeps IFormFile out of Core.</summary>
public record FileUploadInfo(
    Stream  Content,
    string  FileName,
    string  ContentType,
    long    SizeBytes
);
