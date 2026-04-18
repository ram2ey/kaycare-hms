using KayCare.Core.DTOs.IcdCodes;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KayCare.Infrastructure.Services;

public class IcdCodeService : IIcdCodeService
{
    private readonly AppDbContext _db;

    public IcdCodeService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<IcdCodeResponse>> SearchAsync(string query, int limit, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return [];

        var q = query.Trim();

        // Code prefix matches rank first, then description contains
        var results = await _db.IcdCodes
            .Where(c => EF.Functions.Like(c.Code, q + "%") ||
                        EF.Functions.Like(c.Description, "%" + q + "%"))
            .OrderBy(c => EF.Functions.Like(c.Code, q + "%") ? 0 : 1)
            .ThenBy(c => c.Code)
            .Take(limit)
            .Select(c => new IcdCodeResponse(c.Code, c.Description, c.Chapter))
            .AsNoTracking()
            .ToListAsync(ct);

        return results.AsReadOnly();
    }
}
