namespace KayCare.Core.DTOs.Prescriptions;

public class DispenseEventItemResponse
{
    public Guid   PrescriptionItemId { get; set; }
    public string MedicationName     { get; set; } = string.Empty;
    public int    QuantityDispensed  { get; set; }
}

public class DispenseEventResponse
{
    public Guid     DispenseEventId  { get; set; }
    public DateTime DispensedAt      { get; set; }
    public string   DispensedByName  { get; set; } = string.Empty;
    public string?  Notes            { get; set; }
    public List<DispenseEventItemResponse> Items { get; set; } = [];
}
