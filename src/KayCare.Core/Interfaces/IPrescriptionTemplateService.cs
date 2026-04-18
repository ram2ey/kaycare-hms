using KayCare.Core.DTOs.PrescriptionTemplates;

namespace KayCare.Core.Interfaces;

public interface IPrescriptionTemplateService
{
    Task<PrescriptionTemplateDetailResponse> CreateAsync(CreatePrescriptionTemplateRequest request, CancellationToken ct = default);
    Task<PrescriptionTemplateDetailResponse> UpdateAsync(Guid templateId, CreatePrescriptionTemplateRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid templateId, CancellationToken ct = default);
    Task<PrescriptionTemplateDetailResponse> GetByIdAsync(Guid templateId, CancellationToken ct = default);
    Task<IReadOnlyList<PrescriptionTemplateResponse>> GetMyTemplatesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PrescriptionTemplateResponse>> GetSharedTemplatesAsync(CancellationToken ct = default);
}
