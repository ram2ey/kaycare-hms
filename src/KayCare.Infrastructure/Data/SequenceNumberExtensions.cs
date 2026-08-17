using Microsoft.EntityFrameworkCore;

namespace KayCare.Infrastructure.Data;

/// <summary>
/// Shared "PREFIX-{year}-NNNNN" business-identifier sequence generation (invoice/admission/MRN/
/// accession/refund/claim/PO numbers) — finds the highest existing number matching the prefix and
/// increments it. Callers are responsible for their own concurrency safety (an
/// <see cref="DbLockExtensions.AcquireAdvisoryLockAsync"/> call before this, which every existing
/// caller already does around the enclosing transaction) — this only computes the number, it
/// doesn't reserve it.
/// </summary>
public static class SequenceNumberExtensions
{
    public static async Task<int> GetNextSequenceAsync(
        this AppDbContext db, IQueryable<string> existingNumbers, string prefix, CancellationToken ct)
    {
        var last = await existingNumbers
            .Where(n => n.StartsWith(prefix))
            .OrderByDescending(n => n)
            .FirstOrDefaultAsync(ct);

        if (last is not null && int.TryParse(last[prefix.Length..], out var lastNum))
            return lastNum + 1;
        return 1;
    }

    public static async Task<string> GenerateSequenceNumberAsync(
        this AppDbContext db, IQueryable<string> existingNumbers, string prefix, CancellationToken ct)
    {
        var seq = await db.GetNextSequenceAsync(existingNumbers, prefix, ct);
        return $"{prefix}{seq:D5}";
    }
}
