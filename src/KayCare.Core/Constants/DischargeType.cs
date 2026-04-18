namespace KayCare.Core.Constants;

public static class DischargeType
{
    public const string Regular  = "Regular";
    public const string AMA      = "AMA";        // Against Medical Advice
    public const string Transfer = "Transfer";
    public const string Death    = "Death";

    public static readonly string[] All = [Regular, AMA, Transfer, Death];
}
