// ... (Usingler aynı)
using istiklal_karacasu_lorawan.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace lht52.Controllers
{
    public class HomeController : Controller
    {
        private readonly FirebaseService _db;

        public HomeController(FirebaseService db)
        {
            _db = db;
        }

        // ... (Index, GPS, havadurumu actionları aynı) ...
        public IActionResult Index() { return View(); }
        public IActionResult GPS() { return View(); }
        public IActionResult havadurumu() { return View(); }

        // GÜNCELLENEN ACTION: Güvenlik tokenini yoksay ve parametreleri URL'den al
        [HttpPost]
        [IgnoreAntiforgeryToken] // <-- BU SATIR 400 HATASINI ÇÖZER (CSRF Korumasını kapatır)
        public async Task<IActionResult> ToggleTracking([FromQuery] string id, [FromQuery] bool status)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                    return BadRequest("Cihaz ID (id) parametresi boş olamaz.");

                string sessionId = null;
                if (status)
                {
                    // Takip başladığında benzersiz ID oluştur (Örn: S_20250108_143000)
                    sessionId = "S_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                }

                // Servisi çağır
                await _db.UpdateTrackingAsync(id, status, sessionId);

                return Ok(new { success = true, sessionId = sessionId });
            }
            catch (Exception ex)
            {
                // Hata detayını döndür
                return BadRequest("Sunucu Hatası: " + ex.Message);
            }
        }

        // ... (GetWeatherData ve ParseWmoCode aynı kalacak) ...
        [HttpGet] public async Task<IActionResult> GetWeatherData() { /*...*/ return Json(null); }
        private (string, string) ParseWmoCode(int code) { return ("", ""); }
    }
}