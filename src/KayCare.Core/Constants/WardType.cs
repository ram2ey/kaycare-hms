namespace KayCare.Core.Constants;

public static class WardType
{
    public const string General   = "General";
    public const string ICU       = "ICU";
    public const string HDU       = "HDU";
    public const string Maternity = "Maternity";
    public const string Pediatric = "Pediatric";
    public const string Private   = "Private";
    public const string Emergency = "Emergency";
    public const string Surgical  = "Surgical";
    public const string Psychiatric = "Psychiatric";

    public static readonly string[] All =
        [General, ICU, HDU, Maternity, Pediatric, Private, Emergency, Surgical, Psychiatric];
}
