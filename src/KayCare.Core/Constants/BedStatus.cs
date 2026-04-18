namespace KayCare.Core.Constants;

public static class BedStatus
{
    public const string Available   = "Available";
    public const string Occupied    = "Occupied";
    public const string Maintenance = "Maintenance";

    public static readonly string[] All = [Available, Occupied, Maintenance];
}
