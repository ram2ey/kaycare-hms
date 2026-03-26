namespace MediCloud.Core.DTOs.LabResults;

public class LabObservationResponse
{
    public Guid    LabObservationId { get; set; }
    public int     SequenceNumber   { get; set; }
    public string  TestCode         { get; set; } = string.Empty;
    public string  TestName         { get; set; } = string.Empty;
    public string? Value            { get; set; }
    public string? Units            { get; set; }
    public string? ReferenceRange   { get; set; }
    public string? AbnormalFlag     { get; set; }
}
