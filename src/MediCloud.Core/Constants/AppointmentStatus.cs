namespace MediCloud.Core.Constants;

/// <summary>
/// Valid appointment statuses and their allowed transitions:
/// Scheduled → Confirmed | Cancelled | NoShow
/// Confirmed → CheckedIn | Cancelled | NoShow
/// CheckedIn → InProgress | Cancelled
/// InProgress → Completed
/// Completed, Cancelled, NoShow are terminal.
/// </summary>
public static class AppointmentStatus
{
    public const string Scheduled  = "Scheduled";
    public const string Confirmed  = "Confirmed";
    public const string CheckedIn  = "CheckedIn";
    public const string InProgress = "InProgress";
    public const string Completed  = "Completed";
    public const string Cancelled  = "Cancelled";
    public const string NoShow     = "NoShow";

    private static readonly Dictionary<string, string[]> Transitions = new()
    {
        [Scheduled]  = [Confirmed, Cancelled, NoShow],
        [Confirmed]  = [CheckedIn, Cancelled, NoShow],
        [CheckedIn]  = [InProgress, Cancelled],
        [InProgress] = [Completed],
        [Completed]  = [],
        [Cancelled]  = [],
        [NoShow]     = []
    };

    public static bool CanTransition(string from, string to) =>
        Transitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
}
