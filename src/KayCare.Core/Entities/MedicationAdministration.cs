namespace KayCare.Core.Entities;

public class MedicationAdministration : TenantEntity
{
    public Guid    MedicationAdministrationId { get; set; }
    public Guid    PrescriptionId             { get; set; }
    public Guid    PrescriptionItemId         { get; set; }
    public Guid    PatientId                  { get; set; }
    public Guid?   AdmissionId                { get; set; }
    public Guid    AdministeredByUserId       { get; set; }
    public DateTime AdministeredAt            { get; set; }
    public string? DoseGiven                  { get; set; }
    public string? Route                      { get; set; }
    public string  Status                     { get; set; } = "Given";
    public string? Notes                      { get; set; }

    public Prescription     Prescription     { get; set; } = null!;
    public PrescriptionItem PrescriptionItem { get; set; } = null!;
    public Patient          Patient          { get; set; } = null!;
    public User             AdministeredBy   { get; set; } = null!;
    public Admission?       Admission        { get; set; }
}
