namespace KayCare.Core.Constants;

public static class RefundStatus
{
    public const string Pending   = "Pending";
    public const string Processed = "Processed";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All = [Pending, Processed, Cancelled];
}
