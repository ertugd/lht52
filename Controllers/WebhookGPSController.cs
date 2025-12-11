using istiklal_karacasu_lorawan.Models;
using istiklal_karacasu_lorawan.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

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
            if (auth.ToString().StartsWith("X-API-KEY ", StringComparison.OrdinalIgnoreCase))
            {
                var token = auth.ToString().Substring(9).Trim();
                return token == expected;
            }
        }
        return false;
    }

    // YARDIMCI METOT: Türkiye Saatini Getir (Sunucu Linux da olsa Windows da olsa çalışır)
    private DateTime GetTurkeyTime()
    {
        try
        {
            // Windows sunucular için ID
            var trZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, trZone);
        }
        catch
        {
            // Linux/Docker sunucular için ID (IANA)
            var trZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, trZone);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ChirpstackIncomingDto payload)
    {
        if (!ValidateApiKey()) return Unauthorized(new { error = "invalid api key" });

        double? lat = null;
        double? lng = null;
        double bat = 0;

        // 1. Veriyi Ayıkla
        if (payload.Object?.Messages != null)
        {
            var flatList = payload.Object.Messages.SelectMany(x => x).ToList();
            foreach (var item in flatList)
            {
                if (item.MeasurementValue is not null && double.TryParse(item.MeasurementValue.ToString(), out double val))
                {
                    if (item.Type == "Latitude") lat = val;
                    if (item.Type == "Longitude") lng = val;
                    if (item.Type == "Battery") bat = val;
                }
            }
        }

        // 2. Durum Belirleme
        string statusMessage = "Aktif";
        bool gpsValid = true;

        if (lat == null || lng == null || lat == 0 || lng == 0)
        {
            gpsValid = false;
            statusMessage = "Konum Alınamadı (GPS Yok)";
            lat = 0;
            lng = 0;
        }

        // 3. Tarihi Türkiye Saatine Çevir
        DateTime trTime = GetTurkeyTime();

        // 4. Modeli Oluştur
        var gpsData = new GPSModel
        {
            Latitude = lat.Value,
            Longitude = lng.Value,
            Battery = (int)bat,
            DeviceName = payload.DeviceInfo?.DeviceName ?? "Bilinmiyor",

            // BURASI GÜNCELLENDİ: Artık sunucu saatini değil, TR saatini basıyoruz.
            LastUpdate = trTime.ToString("dd.MM.yyyy HH:mm:ss"),

            Hiz = 0,
            Status = statusMessage
        };

        try
        {
            await _db.AddEntryGPSAsync(gpsData, payload.DeviceInfo.DevEui);

            if (gpsValid)
                return Ok(new { status = "Başarılı", device = payload.DeviceInfo.DeviceName });
            else
                return Ok(new { status = "Uyarı: GPS verisi yok, durum güncellendi.", device = payload.DeviceInfo.DeviceName });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Firebase Hatası: {ex.Message}");
        }
    }
}