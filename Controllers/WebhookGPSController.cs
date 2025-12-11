using istiklal_karacasu_lorawan.Models;
using istiklal_karacasu_lorawan.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json; // Required for parsing JsonElement

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
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ChirpstackIncomingDto payload)
    {
        if (!ValidateApiKey()) return Unauthorized(new { error = "invalid api key" });

        // 1. Verileri Ayıklama (Parsing)
        double? lat = null;
        double? lng = null;
        double bat = 0;

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

        // 2. Durum Belirleme (GPS Var mı Yok mu?)
        string statusMessage = "Aktif";
        bool gpsValid = true;

        if (lat == null || lng == null || lat == 0 || lng == 0)
        {
            // GPS verisi yoksa veya hatalıysa
            gpsValid = false;
            statusMessage = "Konum Alınamadı (GPS Yok)";

            // Haritada saçma bir yer göstermemesi veya varsayılan bir noktaya gitmesi için 0 veya son bilinen konum
            lat = 0;
            lng = 0;
        }

        // 3. Model Oluşturma
        var gpsData = new GPSModel
        {
            Latitude = lat.Value,
            Longitude = lng.Value,
            Battery = (int)bat, // GPS çekmese bile batarya verisi gelebilir, bunu kaybetmeyelim
            DeviceName = payload.DeviceInfo?.DeviceName ?? "Bilinmiyor",
            LastUpdate = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
            Hiz = 0,
            Status = statusMessage // Yeni durum mesajı
        };

        try
        {
            // GPS çekmese bile veritabanına yazıyoruz ki kullanıcı cihazın çalıştığını (pilini) görsün
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

    // Helper function to handle System.Text.Json elements safely
    private double? GetDoubleValue(object value)
    {
        try
        {
            if (value is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.Number)
                    return element.GetDouble();
            }
            if (value is double d) return d;
            if (value is int i) return (double)i;

            return null;
        }
        catch
        {
            return null;
        }
    }
}