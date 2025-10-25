using lht52.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace lht52.Controllers;

[ApiController]
[Route("api/telemetry")]
public class TelemetryApiController : ControllerBase
{
    private readonly AppDbContext _db;
    public TelemetryApiController(AppDbContext db) => _db = db;

    [HttpGet("last7days")]
    public async Task<IActionResult> Last7Days()
    {
        var from = DateTime.UtcNow.AddDays(-7);
        var data = await _db.Telemetry
            .Where(t => t.Time >= from)
            .OrderBy(t => t.Time)
            .Select(t => new { t.Time, t.Temperature, t.Humidity })
            .ToListAsync();

        return Ok(data);
    }
}