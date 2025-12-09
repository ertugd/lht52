using Microsoft.AspNetCore.Mvc;
using istiklal_karacasu_lorawan.Services;

namespace istiklal_karacasu_lorawan.Controllers
{
    public class HomeController : Controller
    {
        private readonly WeatherService _weatherService;

        public HomeController(WeatherService weatherService)
        {
            _weatherService = weatherService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult GPS()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetWeatherData()
        {
            var current = await _weatherService.GetCurrentWeatherAsync();
            var hourly = await _weatherService.GetHourlyForecastAsync();
            var daily = await _weatherService.GetDailyForecastAsync();

            return Json(new
            {
                current,
                hourly,
                daily
            });
        }
    }
}