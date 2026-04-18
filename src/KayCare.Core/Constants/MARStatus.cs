namespace KayCare.Core.Constants;

public static class MARStatus
{
    public const string Given        = "Given";
    public const string Held         = "Held";
    public const string Refused      = "Refused";
    public const string NotAvailable = "NotAvailable";

    public static readonly string[] All = [Given, Held, Refused, NotAvailable];
}
