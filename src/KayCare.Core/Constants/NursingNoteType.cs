namespace KayCare.Core.Constants;

public static class NursingNoteType
{
    public const string General    = "General";
    public const string Assessment = "Assessment";
    public const string Handover   = "Handover";
    public const string Procedure  = "Procedure";

    public static readonly string[] All = [General, Assessment, Handover, Procedure];
}
