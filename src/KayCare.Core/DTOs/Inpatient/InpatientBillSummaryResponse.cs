namespace KayCare.Core.DTOs.Inpatient;

public class AccommodationSegment
{
    public string   WardName  { get; set; } = string.Empty;
    public string   WardType  { get; set; } = string.Empty;
    public DateTime From      { get; set; }
    public DateTime To        { get; set; }
    public int      Days      { get; set; }
    public decimal  DailyRate { get; set; }
    public decimal  Total     { get; set; }
}

public class InpatientBillSummaryResponse
{
    public Guid     AdmissionId       { get; set; }
    public string   AdmissionNumber   { get; set; } = string.Empty;
    public string   PatientName       { get; set; } = string.Empty;
    public string   PatientMrn        { get; set; } = string.Empty;
    public decimal  TotalCharges      { get; set; }
    public Guid?    BillId            { get; set; }
    public string?  BillNumber        { get; set; }
    public string?  BillStatus        { get; set; }
    public List<AccommodationSegment>      AccommodationBreakdown { get; set; } = [];
    public List<InpatientChargeResponse>   Charges                { get; set; } = [];
}
