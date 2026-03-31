namespace KayCare.Core.Constants;

public static class RefundMethod
{
    public const string Cash         = "Cash";
    public const string BankTransfer = "BankTransfer";
    public const string MobileMoney  = "MobileMoney";
    public const string Cheque       = "Cheque";

    public static readonly string[] All = [Cash, BankTransfer, MobileMoney, Cheque];
}
