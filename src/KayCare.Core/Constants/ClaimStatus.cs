namespace KayCare.Core.Constants;

public static class ClaimStatus
{
    public const string Draft              = "Draft";
    public const string Submitted          = "Submitted";
    public const string Approved           = "Approved";
    public const string PartiallyApproved  = "PartiallyApproved";
    public const string Rejected           = "Rejected";
    public const string Cancelled          = "Cancelled";

    public static readonly string[] All =
        [Draft, Submitted, Approved, PartiallyApproved, Rejected, Cancelled];
}
