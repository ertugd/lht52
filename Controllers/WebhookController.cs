using lht52;
using lht52.Data;
using lht52.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json.Linq;

namespace ChirpStackViewer.Controllers;

[ApiController]
[Route("api/webhook")]
public class WebhookController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly AppDbContext _db;
    private readonly IHubContext<TelemetryHub> _hub;

    public WebhookController(IConfiguration config, AppDbContext db, IHubContext<TelemetryHub> hub)
    {
        _config = config;
        _db = db;
        _hub = hub;
    }

    private bool ValidateApiKey()
    {
        var expected = _config.GetValue<string>("X-API-KEY");
        if (string.IsNullOrEmpty(expected)) return false;
        if (Request.Headers.TryGetValue("X-API-KEY", out var v) && v == expected) return true;
        if (Request.Headers.TryGetValue("Authorization", out var auth))
        {
            if (auth.ToString().StartsWith("X-API-KEY ", StringComparison.OrdinalIgnoreCase))
            {
                var token = auth.ToString().Substring(7).Trim();
                return token == expected;
            }
        }
        return false;
    }

    [HttpPost]
    public async Task<IActionResult> Post()
    {
        if (!ValidateApiKey()) return Unauthorized(new { error = "invalid api key" });

        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(body)) return BadRequest();

        try
        {
            var j = JObject.Parse(body);
            var obj = j["object"] as JObject;
            var entry = new TelemetryEntry
            {
                RawJson = body,
                Time = j.Value<DateTime?>("time") ?? DateTime.UtcNow,
                Temperature = (double?)obj?["TempC_SHT"] ?? (double?)obj?["TempC_DS"],
                Humidity = (double?)obj?["Hum_SHT"]
            };
            _db.Telemetry.Add(entry);
            await _db.SaveChangesAsync();

            await _hub.Clients.All.SendAsync("TelemetryUpdated");

            return Ok(new { status = "stored" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}