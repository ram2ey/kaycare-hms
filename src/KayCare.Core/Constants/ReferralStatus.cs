namespace KayCare.Core.Constants;

public static class ReferralStatus
{
    public const string Draft     = "Draft";
    public const string Sent      = "Sent";
    public const string Accepted  = "Accepted";
    public const string Completed = "Completed";
    public const string Declined  = "Declined";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All = [Draft, Sent, Accepted, Completed, Declined, Cancelled];
}
