using MediCloud.Core.DTOs.Documents;

namespace MediCloud.Core.Interfaces;

public interface IDocumentService
{
    Task<DocumentResponse> UploadAsync(UploadDocumentRequest request, FileUploadInfo file, CancellationToken ct = default);
    Task<IReadOnlyList<DocumentResponse>> GetByPatientAsync(Guid patientId, CancellationToken ct = default);
    Task<string> GetDownloadUrlAsync(Guid documentId, CancellationToken ct = default);
    Task DeleteAsync(Guid documentId, CancellationToken ct = default);
}
