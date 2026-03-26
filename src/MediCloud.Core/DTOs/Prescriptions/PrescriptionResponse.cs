namespace MediCloud.Core.DTOs.Prescriptions;

public class PrescriptionResponse
{
    public Guid     PrescriptionId     { get; set; }
    public Guid     ConsultationId     { get; set; }
    public Guid     PatientId          { get; set; }
    public string   PatientName        { get; set; } = string.Empty;
    public string   MedicalRecordNumber{ get; set; } = string.Empty;
    public Guid     PrescribedByUserId { get; set; }
    public string   PrescribedByName   { get; set; } = string.Empty;
    public DateOnly PrescriptionDate   { get; set; }
    public string   Status             { get; set; } = string.Empty;
    public int      ItemCount          { get; set; }
    public bool     HasControlledSubstances { get; set; }
    public DateTime CreatedAt          { get; set; }
}
