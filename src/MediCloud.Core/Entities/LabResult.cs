namespace MediCloud.Core.Entities;

public class LabResult : TenantEntity
{
    public Guid   LabResultId          { get; set; }
    public Guid   PatientId            { get; set; }
    public Guid?  OrderingDoctorUserId { get; set; }

    public string  AccessionNumber { get; set; } = string.Empty; // OBR-3
    public string? OrderCode       { get; set; }                  // OBR-4 component 1
    public string? OrderName       { get; set; }                  // OBR-4 component 2
    public DateTime? OrderedAt     { get; set; }                  // OBR-6
    public DateTime  ReceivedAt    { get; set; }                  // OBR-22

    public string  Status  { get; set; } = "Received";            // Received | Verified
    public string? RawHl7  { get; set; }                          // original MLLP payload

    /// <summary>Set by MllpListenerService when accession matches a pending LabOrderItem.</summary>
    public Guid? LabOrderItemId { get; set; }

    // Navigation
    public Patient             Patient        { get; set; } = null!;
    public User?               OrderingDoctor { get; set; }
    public LabOrderItem?       LabOrderItem   { get; set; }
    public ICollection<LabObservation> Observations { get; set; } = [];
}
