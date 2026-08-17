using System.Security.Claims;
using KayCare.Core.Constants;

namespace KayCare.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>True if the user is Admin or SuperAdmin — the "full access" tier used across
    /// several controllers to gate write operations beyond what [Authorize(Roles=...)] alone
    /// expresses (e.g. seeing inactive rows, or a business rule finer than role-per-endpoint).</summary>
    public static bool IsAdminOrSuperAdmin(this ClaimsPrincipal user) =>
        user.IsInRole(Roles.Admin) || user.IsInRole(Roles.SuperAdmin);
}
