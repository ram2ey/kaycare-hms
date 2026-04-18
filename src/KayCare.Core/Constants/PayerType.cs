namespace KayCare.Core.Constants;

public static class PayerType
{
    public const string NHIS             = "NHIS";
    public const string PrivateInsurance = "PrivateInsurance";
    public const string Corporate        = "Corporate";
    public const string Government       = "Government";

    public static readonly string[] All = [NHIS, PrivateInsurance, Corporate, Government];
}
