namespace MediCloud.Core.Entities;

public class Prescription : TenantEntity
{
    public Guid      PrescriptionId     { get; set; }
    public Guid      ConsultationId     { get; set; }
    public Guid      PatientId          { get; set; }
    public Guid      PrescribedByUserId { get; set; }
    public DateOnly  PrescriptionDate   { get; set; }
    public string    Status             { get; set; } = "Active";
    public string?   Notes              { get; set; }
    public DateTime? DispensedAt        { get; set; }
    public Guid?     DispensedByUserId  { get; set; }

    public Patient  Patient      { get; set; } = null!;
    public User     PrescribedBy { get; set; } = null!;
    public User?    DispensedBy  { get; set; }

    public ICollection<PrescriptionItem> Items { get; set; } = [];
}
