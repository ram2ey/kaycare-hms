namespace KayCare.Core.Constants;

public static class DischargeCondition
{
    public const string Stable       = "Stable";
    public const string Improved     = "Improved";
    public const string Unchanged    = "Unchanged";
    public const string Deteriorated = "Deteriorated";
    public const string Deceased     = "Deceased";

    public static readonly string[] All =
        [Stable, Improved, Unchanged, Deteriorated, Deceased];
}
