namespace KayCare.Core.DTOs.PrescriptionTemplates;

public class PrescriptionTemplateDetailResponse : PrescriptionTemplateResponse
{
    public List<PrescriptionTemplateItemResponse> Items { get; set; } = [];
}
