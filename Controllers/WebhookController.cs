using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using istiklal_karacasu_lorawan.Models;
using istiklal_karacasu_lorawan.Services;
using System;
using System.Threading.Tasks;

namespace istiklal_karacasu_lorawan.Controllers
{
    [ApiController]
    [Route("[controller]")] // Web adresi "site.com/Webhook" şeklinde olacak
    public class WebhookController : ControllerBase
    {
        private readonly IFirebaseService _firebaseService;
        private readonly string _webhookApiKey;

        public WebhookController(IFirebaseService firebaseService, IConfiguration configuration)
        {
            _firebaseService = firebaseService;
            _webhookApiKey = configuration["WEBHOOK_API_KEY"] ?? "istiklal_secret_token_2026";
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] JObject payload)
        {
            var apiKey = Request.Headers["X-API-Key"].ToString();
            if (string.IsNullOrEmpty(apiKey))
            {
                apiKey = Request.Query["key"].ToString();
            }

            if (apiKey != _webhookApiKey)
            {
                return Unauthorized(new { status = "error", message = "Yetkisiz Erişim (Şifre Yanlış)" });
            }

            if (payload == null)
            {
                return BadRequest(new { status = "error", message = "Hiçbir veri alınmadı" });
            }

            try
            {
                var devEui = payload["deviceInfo"]?["devEui"]?.ToString();
                var deviceName = payload["deviceInfo"]?["deviceName"]?.ToString();
                var timestamp = payload["time"]?.ToString();
                var payloadObj = payload["object"];

                if (string.IsNullOrEmpty(devEui))
                {
                    return BadRequest(new { status = "error", message = "Cihaz ID (devEui) bulunamadı" });
                }

                if (payloadObj != null)
                {
                    // 1. İHTİMAL: Isı ve Nem sensörü (LHT52)
                    if (payloadObj["TempC_SHT"] != null)
                    {
                        double? tempInner = payloadObj["TempC_SHT"]?.Value<double>();
                        double? tempOuter = payloadObj["TempC_DS"]?.Value<double>();
                        double? humidity = payloadObj["Hum_SHT"]?.Value<double>();
                        double? battery = payloadObj["Battery"]?.Value<double>();

                        await _firebaseService.SaveTempHumAsync(devEui, deviceName, tempInner, tempOuter, humidity, battery, timestamp);
                    }
                    // 2. İHTİMAL: GPS konum sensörü (T1000)
                    else if (payloadObj["messages"] != null)
                    {
                        var messages = payloadObj["messages"]?[0];
                        double? lat = null;
                        double? lng = null;
                        double? battery = null;

                        if (messages != null)
                        {
                            foreach (var msg in messages)
                            {
                                var type = msg["type"]?.ToString();
                                if (type == "Latitude") lat = msg["measurementValue"]?.Value<double>();
                                else if (type == "Longitude") lng = msg["measurementValue"]?.Value<double>();
                                else if (type == "Battery") battery = msg["measurementValue"]?.Value<double>();
                            }
                        }

                        if (lat.HasValue && lng.HasValue)
                        {
                            await _firebaseService.SaveGpsAsync(devEui, deviceName, lat.Value, lng.Value, battery, timestamp);
                        }
                    }
                }

                return Ok(new { status = "success" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Webhook işlenirken hata oluştu: {ex.Message}");
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }
    }
}
