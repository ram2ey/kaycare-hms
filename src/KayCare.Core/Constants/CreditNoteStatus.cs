namespace KayCare.Core.Constants;

public static class CreditNoteStatus
{
    public const string Draft    = "Draft";
    public const string Approved = "Approved";
    public const string Applied  = "Applied";
    public const string Voided   = "Voided";

    public static readonly string[] All = [Draft, Approved, Applied, Voided];
}
