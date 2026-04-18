namespace KayCare.Core.DTOs.Inpatient;

public class SaveInpatientChargeRequest
{
    public DateOnly ChargeDate  { get; set; }
    public string   Category    { get; set; } = string.Empty;
    public string   Description { get; set; } = string.Empty;
    public int      Quantity    { get; set; } = 1;
    public decimal  UnitPrice   { get; set; }
    public string?  Notes       { get; set; }
}
