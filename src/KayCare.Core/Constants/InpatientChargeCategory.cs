namespace KayCare.Core.Constants;

public static class InpatientChargeCategory
{
    public const string Accommodation = "Accommodation";
    public const string Procedure     = "Procedure";
    public const string Medication    = "Medication";
    public const string Lab           = "Lab";
    public const string Other         = "Other";

    public static readonly string[] All =
        [Accommodation, Procedure, Medication, Lab, Other];
}
