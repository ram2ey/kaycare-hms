namespace MediCloud.Core.Entities;

/// <summary>
/// One OBX segment — a single test result line within a lab order.
/// Does NOT inherit TenantEntity (no timestamps); TenantId set manually.
/// </summary>
public class LabObservation
{
    public Guid LabObservationId { get; set; }
    public Guid LabResultId      { get; set; }
    public Guid TenantId         { get; set; }

    public int     SequenceNumber  { get; set; }  // OBX-1
    public string  TestCode        { get; set; } = string.Empty; // OBX-3 component 1
    public string  TestName        { get; set; } = string.Empty; // OBX-3 component 2
    public string? Value           { get; set; }  // OBX-5
    public string? Units           { get; set; }  // OBX-6
    public string? ReferenceRange  { get; set; }  // OBX-7
    public string? AbnormalFlag    { get; set; }  // OBX-8  (H / L / N / A / LL / HH)

    // Navigation
    public LabResult LabResult { get; set; } = null!;
}
