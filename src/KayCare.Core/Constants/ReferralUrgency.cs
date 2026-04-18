namespace KayCare.Core.Constants;

public static class ReferralUrgency
{
    public const string Routine   = "Routine";
    public const string Urgent    = "Urgent";
    public const string Emergency = "Emergency";

    public static readonly string[] All = [Routine, Urgent, Emergency];
}
