namespace KayCare.Core.Entities;

public class VitalSigns : TenantEntity
{
    public Guid      VitalSignsId    { get; set; }
    public Guid      PatientId       { get; set; }
    public Guid?     AdmissionId     { get; set; }
    public Guid?     ConsultationId  { get; set; }
    public Guid      RecordedByUserId { get; set; }
    public DateTime  RecordedAt      { get; set; }

    public int?     BloodPressureSystolic  { get; set; }
    public int?     BloodPressureDiastolic { get; set; }
    public int?     PulseRate              { get; set; }
    public decimal? Temperature            { get; set; }
    public int?     SpO2                   { get; set; }
    public int?     RespiratoryRate        { get; set; }
    public decimal? Weight                 { get; set; }
    public decimal? Height                 { get; set; }
    public string?  Notes                  { get; set; }

    public Patient   Patient      { get; set; } = null!;
    public User      RecordedBy   { get; set; } = null!;
    public Admission? Admission   { get; set; }
}
