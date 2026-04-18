using KayCare.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KayCare.API.Controllers;

[ApiController]
[Route("api/icd-codes")]
[Authorize]
public class IcdCodesController : ControllerBase
{
    private readonly IIcdCodeService _service;

    public IcdCodesController(IIcdCodeService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string q = "",
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        var results = await _service.SearchAsync(q, Math.Clamp(limit, 1, 50), ct);
        return Ok(results);
    }
}
