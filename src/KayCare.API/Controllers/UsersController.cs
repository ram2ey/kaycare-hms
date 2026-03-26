using KayCare.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KayCare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>List active users, optionally filtered by role name.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? role, CancellationToken ct)
    {
        var query = _db.Users
            .Include(u => u.Role)
            .Where(u => u.IsActive);

        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(u => u.Role.RoleName == role);

        var users = await query
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Select(u => new
            {
                u.UserId,
                FullName = u.FirstName + " " + u.LastName,
                u.Email,
                Role = u.Role.RoleName,
                u.Department,
                u.LicenseNumber,
            })
            .ToListAsync(ct);

        return Ok(users);
    }
}
