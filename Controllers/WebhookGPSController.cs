using istiklal_karacasu_lorawan.Models;
using istiklal_karacasu_lorawan.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Globalization;

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

    // ... (ValidateApiKey ve GetTurkeyTime metodları aynen kalacak) ...
    private bool ValidateApiKey() { /* Mevcut kodlar... */ return true; /*Burayı kısaltıyorum, sizdeki kalsın*/ }

    // GÜNCELLENDİ: Tarih formatı parse işlemi için
    // --- DÜZELTİLEN METOT ---
    private DateTime GetTurkeyTime(DateTime date)
    {
        // Gelen tarih "Local" ise UTC'ye çeviriyoruz.
        // "Unspecified" (Belirsiz) ise UTC olduğunu varsayıyoruz.
        if (date.Kind == DateTimeKind.Local)
        {
            date = date.ToUniversalTime();
        }
        else if (date.Kind == DateTimeKind.Unspecified)
        {
            date = DateTime.SpecifyKind(date, DateTimeKind.Utc);
        }

        try
        {
            var trZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(date, trZone);
        }
        catch
        {
            var trZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
            return TimeZoneInfo.ConvertTimeFromUtc(date, trZone);
        }
    }

    // YENİ METOT: İki nokta arasındaki mesafeyi (km) hesaplar
    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        if (lat1 == lat2 && lon1 == lon2) return 0;

        var r = 6371; // Dünya yarıçapı (km)
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return r * c;
    }

    private double ToRadians(double angle)
    {
        return Math.PI * angle / 180.0;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ChirpstackIncomingDto payload)
    {
        if (!ValidateApiKey()) return Unauthorized(new { error = "invalid api key" });

        double? lat = null;
        double? lng = null;
        double bat = 0;

        if (payload.Object?.Messages != null)
        {
            var flatList = payload.Object.Messages.SelectMany(x => x).ToList();
            foreach (var item in flatList)
            {
                if (item.MeasurementValue is not null)
                {
                    string valString = item.MeasurementValue.ToString().Replace(",", ".");
                    if (double.TryParse(valString, NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
                    {
                        if (item.Type == "Latitude") lat = val;
                        if (item.Type == "Longitude") lng = val;
                        if (item.Type == "Battery") bat = val;
                    }
                }
            }
        }

        string statusMessage = "Aktif";
        bool gpsValid = true;

        if (lat == null || lng == null || lat == 0 || lng == 0)
        {
            gpsValid = false;
            statusMessage = "Konum Alınamadı (GPS Yok)";
            lat = 0;
            lng = 0;
        }

        // --- TARİHİ HAZIRLAMA ---
        // Gelen zamanı al, yoksa şu anı kullan.
        DateTime incomingDate = payload.Time == DateTime.MinValue ? DateTime.UtcNow : payload.Time;

        // Yukarıda düzelttiğimiz güvenli metodu çağırıyoruz
        DateTime trTime = GetTurkeyTime(incomingDate);

        // HIZ VE SESSION HESAPLAMA
        double calculatedSpeed = 0;
        bool isTracking = false;
        string currentSessionId = null;

        try
        {
            var prevData = await _db.GetGPSAsync(payload.DeviceInfo.DevEui);
            if (prevData != null)
            {
                isTracking = prevData.IsTracking;
                currentSessionId = prevData.SessionId;

                if (gpsValid && prevData.Latitude != 0 && prevData.Longitude != 0)
                {
                    // Eski tarih formatını parse et
                    if (DateTime.TryParseExact(prevData.LastUpdate, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime prevTime))
                    {
                        double distanceKm = CalculateDistance(prevData.Latitude, prevData.Longitude, lat.Value, lng.Value);
                        double timeDiffHours = (trTime - prevTime).TotalHours;

                        if (timeDiffHours > 0.0001 && distanceKm > 0)
                        {
                            calculatedSpeed = distanceKm / timeDiffHours;
                        }
                    }
                }
            }
        }
        catch { }

        var gpsData = new GPSModel
        {
            Latitude = lat.Value,
            Longitude = lng.Value,
            Battery = (int)bat,
            DeviceName = payload.DeviceInfo?.DeviceName ?? "Bilinmiyor",
            LastUpdate = trTime.ToString("dd.MM.yyyy HH:mm:ss"),
            Hiz = Math.Round(calculatedSpeed, 1),
            Status = statusMessage,
            IsTracking = isTracking,
            SessionId = currentSessionId
        };

        try
        {
            await _db.AddEntryGPSAsync(gpsData, payload.DeviceInfo.DevEui);
            return Ok(new { status = "Başarılı", speed = gpsData.Hiz });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Firebase Hatası: {ex.Message}");
        }
    }
}