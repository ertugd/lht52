using istiklal_karacasu_lorawan.Models;
using istiklal_karacasu_lorawan.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace istiklal_karacasu_lorawan.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly FirebaseService _firebaseService;

        public HomeController(FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
            //AddTestData();
        }

        public async Task<IActionResult> Index()
        {          
           return View();
        }

        // Firebase’e test verisi ekleme
        [HttpPost]
        public async Task<IActionResult> AddTestData()
        {
            var random = new Random();
            var entry = new TelemetryEntry
            {
                Time = DateTime.UtcNow,
                Temperature = Math.Round(20 + random.NextDouble() * 5, 2),
                Humidity = Math.Round(40 + random.NextDouble() * 20, 2),
                RawJson = "{}"
            };

            await _firebaseService.AddEntryAsync(entry);
            return Ok(entry);
        }
    }
}


