using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace IstiklalLorawanAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WeatherController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiKey;
        private readonly string _city;
        private readonly string? _latitude;
        private readonly string? _longitude;

        public WeatherController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _apiKey = configuration["WEATHER_API_KEY"] ?? "d5f0325a9dfb9517d0af48e4ca027a18";
            _city = configuration["CITY"] ?? "Karacasu,TR";
            _latitude = configuration["WEATHER_LATITUDE"];
            _longitude = configuration["WEATHER_LONGITUDE"];
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = !string.IsNullOrEmpty(_latitude) && !string.IsNullOrEmpty(_longitude)
                    ? $"http://api.openweathermap.org/data/2.5/weather?lat={_latitude}&lon={_longitude}&appid={_apiKey}&units=metric&lang=tr"
                    : $"http://api.openweathermap.org/data/2.5/weather?q={_city}&appid={_apiKey}&units=metric&lang=tr";
                
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject(content);
                    return Ok(data);
                }
                
                return StatusCode((int)response.StatusCode, new { status = "error", message = "Failed to fetch weather" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }
    }
}
