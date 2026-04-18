using KayCare.Core.Constants;

namespace KayCare.Core.Entities;

public class Bed : TenantEntity
{
    public Guid    BedId     { get; set; }
    public Guid    WardId    { get; set; }
    public string  BedNumber { get; set; } = string.Empty;
    public string  Status    { get; set; } = BedStatus.Available;
    public string? Notes     { get; set; }

    public Ward Ward { get; set; } = null!;
}
