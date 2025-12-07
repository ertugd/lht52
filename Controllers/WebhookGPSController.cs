
using istiklal_karacasu_lorawan.Models;
using istiklal_karacasu_lorawan.Services;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class WebhookGPSController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly FirebaseService _db;

    public WebhookGPSController(IConfiguration config, FirebaseService db)
    {
        _config = config;
        _db = db;
    }

    private bool ValidateApiKey()
    {
        var expected = _config.GetValue<string>("X-API-KEY");
        if (string.IsNullOrEmpty(expected)) return false;
        if (Request.Headers.TryGetValue("X-API-KEY", out var v) && v == expected) return true;
        if (Request.Headers.TryGetValue("Authorization", out var auth))
        {
            if (auth.ToString().StartsWith("X-API-KEY ", System.StringComparison.OrdinalIgnoreCase))
            {
                var token = auth.ToString().Substring(9).Trim();
                return token == expected;
            }
        }
        return false;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ChirpstackIncomingDto payload)
    {
        if (!ValidateApiKey()) return Unauthorized(new { error = "invalid api key" });

        // GPS verisi bazen '0' veya 'null' gelebilir (uydu çekmezse), bunları filtrele
        if (payload.Object.latitude == null || payload.Object.longitude == null ||
            payload.Object.latitude == 0)
        {
            // GPS yoksa sadece log düş, veritabanını bozma
            return Ok(new { status = "GPS verisi yok, pas geçildi." });
        }

        // 2. GpsModel Oluşturma (Mapping)
        // ChirpStack verisini kendi temiz modelimize dönüştürüyoruz
        var gpsData = new GPSModel
        {
            Latitude = payload.Object.latitude.Value,
            Longitude = payload.Object.longitude.Value,
            Battery = payload.Object.battery ?? 0,
            DeviceName = payload.DeviceInfo.DeviceName,
            LastUpdate = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
            Hiz = 0 // SenseCAP L-1000B hız verisi göndermezse manuel hesaplanabilir veya 0 girilir.
        };

        try
        {
            await _db.AddEntryGPSAsync(gpsData, payload.DeviceInfo.DevEui);

            return Ok(new { status = "Başarılı", device = payload.DeviceInfo.DeviceName });
        }
        catch (Exception ex)
        {
            // Hata durumunda loglama yapabilirsin
            return StatusCode(500, $"Firebase Hatası: {ex.Message}");
        }
    }
}