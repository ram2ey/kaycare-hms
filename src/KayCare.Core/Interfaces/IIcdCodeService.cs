using KayCare.Core.DTOs.IcdCodes;

namespace KayCare.Core.Interfaces;

public interface IIcdCodeService
{
    Task<IReadOnlyList<IcdCodeResponse>> SearchAsync(string query, int limit, CancellationToken ct);
}
