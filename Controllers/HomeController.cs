using Microsoft.AspNetCore.Mvc;
using istiklal_karacasu_lorawan.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace istiklal_karacasu_lorawan.Controllers
{
    public class HomeController : Controller
    {
        private readonly FirebaseService _firebaseService;

        public HomeController(FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        public async Task<IActionResult> Index()
        {
            DateTime now = DateTime.Now;
            DateTime oneWeekAgo = now.AddDays(-7);

            var entries = await _firebaseService.GetEntriesAsync(oneWeekAgo, now);

            // Kullanıcıya gösterilecek okunabilir tarihler
            ViewBag.Times = entries
                .Select(e => e.Time.ToLocalTime().ToString("dd.MM HH:mm"))
                .ToList();

            ViewBag.Temperatures = entries
                .Select(e => e.Temperature ?? 0)
                .ToList();

            ViewBag.Humidities = entries
                .Select(e => e.Humidity ?? 0)
                .ToList();

            ViewBag.FirebaseUrl = _firebaseService.BaseUrl;

            // Firebase listener için en son zaman (ISO string)
            ViewBag.EntriesLastTime = entries.Any()
                ? entries.Max(e => e.Time).ToUniversalTime().ToString("o")
                : DateTime.UtcNow.ToString("o");

            return View();
        }

        public async Task<IActionResult> Privacy()
        {
            return View();
        }
    }
}
