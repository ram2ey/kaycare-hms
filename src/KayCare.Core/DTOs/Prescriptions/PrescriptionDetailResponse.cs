namespace KayCare.Core.DTOs.Prescriptions;

public class PrescriptionDetailResponse : PrescriptionResponse
{
    public string?   Notes            { get; set; }
    public DateTime? DispensedAt      { get; set; }
    public string?   DispensedByName  { get; set; }
    public DateTime  UpdatedAt        { get; set; }
    public List<PrescriptionItemResponse> Items { get; set; } = [];
}
