namespace KayCare.Core.Entities;

public class AdmissionTransfer
{
    public Guid     AdmissionTransferId  { get; set; }
    public Guid     TenantId             { get; set; }
    public Guid     AdmissionId          { get; set; }
    public Guid     FromBedId            { get; set; }
    public Guid     FromWardId           { get; set; }
    public Guid     ToBedId              { get; set; }
    public Guid     ToWardId             { get; set; }
    public DateTime TransferredAt        { get; set; }
    public string?  Reason               { get; set; }
    public Guid     TransferredByUserId  { get; set; }

    public Admission Admission           { get; set; } = null!;
    public Bed       FromBed             { get; set; } = null!;
    public Ward      FromWard            { get; set; } = null!;
    public Bed       ToBed               { get; set; } = null!;
    public Ward      ToWard              { get; set; } = null!;
    public User      TransferredBy       { get; set; } = null!;
}
