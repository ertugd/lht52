using istiklal_karacasu_lorawan.Models;
using istiklal_karacasu_lorawan.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Globalization; // 1. BU KÜTÜPHANEYİ EKLEDİK

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

    private DateTime GetTurkeyTime(DateTime utcDate)
    {
        try
        {
            var trZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(utcDate, trZone);
        }
        catch
        {
            var trZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
            return TimeZoneInfo.ConvertTimeFromUtc(utcDate, trZone);
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
                // 2. PARSE İŞLEMİ GÜNCELLENDİ
                // Gelen değer null değilse işlem yap
                if (item.MeasurementValue is not null)
                {
                    string valString = item.MeasurementValue.ToString();

                    // Önlem: Eğer sunucu Türkçe çalışıyorsa ve gelen veri bir şekilde virgüllü geldiyse
                    // veya tam tersi durumlar için her şeyi noktaya çevirip InvariantCulture ile parse ediyoruz.
                    valString = valString.Replace(",", ".");

                    // NumberStyles.Any ve CultureInfo.InvariantCulture kullanarak
                    // noktanın her zaman ondalık ayracı olmasını garantiye alıyoruz.
                    if (double.TryParse(valString, NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
                    {
                        if (item.Type == "Latitude") lat = val;
                        if (item.Type == "Longitude") lng = val;
                        if (item.Type == "Battery") bat = val;
                    }
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

        DateTime incomingUtc = payload.Time == DateTime.MinValue ? DateTime.UtcNow : payload.Time.ToUniversalTime();
        DateTime trTime = GetTurkeyTime(incomingUtc);

        var gpsData = new GPSModel
        {
            Latitude = lat.Value,
            Longitude = lng.Value,
            Battery = (int)bat,
            DeviceName = payload.DeviceInfo?.DeviceName ?? "Bilinmiyor",
            LastUpdate = trTime.ToString("dd.MM.yyyy HH:mm:ss"),
            Hiz = 0,
            Status = statusMessage,
            IsTracking = false
        };

        try
        {
            var existingDevice = await _db.GetGPSAsync(payload.DeviceInfo.DevEui);
            if (existingDevice != null)
            {
                gpsData.IsTracking = existingDevice.IsTracking;
            }

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