using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using IstiklalLorawanAPI.Models;
using IstiklalLorawanAPI.Services;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace IstiklalLorawanAPI.Controllers
{
    // Bu sınıf, dışarıdan gelen verileri yakalayan bir kapı (API Ucu) görevi görür.
    [ApiController]
    [Route("[controller]")] // Web adresi "site.com/Webhook" şeklinde olacak
    public class WebhookController : ControllerBase
    {
        // Veritabanı (Firebase) işlemlerini yapacak servis
        private readonly IFirebaseService _firebaseService;
        // Gelen verilerin gerçekten bizim sensörlerimizden mi geldiğini anlamak için gizli şifre
        private readonly string _webhookApiKey;

        // Sınıf oluşturulduğunda ilk çalışan ayar bölümü
        public WebhookController(IFirebaseService firebaseService, IConfiguration configuration)
        {
            _firebaseService = firebaseService;
            // Ayarlar dosyasından şifreyi al, bulamazsan varsayılan şifreyi kullan
            _webhookApiKey = configuration["WEBHOOK_API_KEY"] ?? "istiklal_secret_token_2026";
        }

        // Sensör ağı (ChirpStack) buraya POST (Gönderme) işlemiyle yeni veri yolladığında bu fonksiyon çalışır.
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] JObject payload) // payload = Gelen veri paketi
        {
            // --- GÜVENLİK KONTROLÜ ---
            // İsteğin başlığından veya adres çubuğundan şifreyi (API Key) arıyoruz
            var apiKey = Request.Headers["X-API-Key"].ToString();
            if (string.IsNullOrEmpty(apiKey))
            {
                apiKey = Request.Query["key"].ToString();
            }

            // Gelen şifre bizim şifremizle uyuşmuyor mu?
            if (apiKey != _webhookApiKey)
            {
                // Uyuşmuyorsa hata ver ve işlemi reddet (Unauthorized: Yetkisiz Giriş)
                return Unauthorized(new { status = "error", message = "Yetkisiz Erişim (Şifre Yanlış)" });
            }

            // Eğer paket boş geldiyse hata ver
            if (payload == null)
            {
                return BadRequest(new { status = "error", message = "Hiçbir veri alınmadı" });
            }

            try
            {
                // --- VERİYİ PARÇALAMA ---
                // Gelen karmaşık JSON paketi içinden ihtiyacımız olan kısımları çekiyoruz
                var devEui = payload["deviceInfo"]?["devEui"]?.ToString(); // Cihazın kimlik numarası
                var deviceName = payload["deviceInfo"]?["deviceName"]?.ToString(); // Cihazın adı
                var timestamp = payload["time"]?.ToString(); // Verinin okunduğu saat
                var payloadObj = payload["object"]; // Asıl ölçüm değerleri

                // Eğer cihaz numarası yoksa kime ait olduğunu bilemeyiz, hata ver
                if (string.IsNullOrEmpty(devEui))
                {
                    return BadRequest(new { status = "error", message = "Cihaz ID (devEui) bulunamadı" });
                }

                // Ölçüm değerleri boş değilse işlemlere başla
                if (payloadObj != null)
                {
                    // 1. İHTİMAL: Bu cihaz bir Isı ve Nem sensörü mü? (İçinde 'TempC_SHT' var mı?)
                    if (payloadObj["TempC_SHT"] != null)
                    {
                        // Sıcaklık, nem ve batarya oranlarını ondalıklı sayı (double) olarak alıyoruz
                        double? tempInner = payloadObj["TempC_SHT"]?.Value<double>();
                        double? tempOuter = payloadObj["TempC_DS"]?.Value<double>();
                        double? humidity = payloadObj["Hum_SHT"]?.Value<double>();
                        double? battery = payloadObj["Battery"]?.Value<double>();

                        // Veritabanına kaydetmesi için Firebase servisine gönderiyoruz
                        await _firebaseService.SaveTempHumAsync(devEui, deviceName, tempInner, tempOuter, humidity, battery, timestamp);
                    }
                    // 2. İHTİMAL: Bu cihaz bir GPS konum sensörü mü? (İçinde 'messages' var mı?)
                    else if (payloadObj["messages"] != null)
                    {
                        var messages = payloadObj["messages"]?[0];
                        double? lat = null; // Enlem
                        double? lng = null; // Boylam
                        double? battery = null; // Batarya

                        // Mesajların içinde dönerek ilgili kısımları bul
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

                        // Eğer hem enlem hem boylam bulabildiysek, konumu veritabanına kaydet
                        if (lat.HasValue && lng.HasValue)
                        {
                            await _firebaseService.SaveGpsAsync(devEui, deviceName, lat.Value, lng.Value, battery, timestamp);
                        }
                    }
                }

                // İşlem başarılıysa "Tamam (200 OK)" cevabı gönder
                return Ok(new { status = "success" });
            }
            catch (Exception ex)
            {
                // Beklenmedik bir hata olursa sunucu çökmesin diye hatayı yakala ve ekrana yazdır
                Console.WriteLine($"Webhook işlenirken hata oluştu: {ex.Message}");
                // 500: Sunucu Hatası döndür
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }
    }
}
