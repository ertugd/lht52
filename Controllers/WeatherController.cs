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

        public WeatherController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _apiKey = configuration["WEATHER_API_KEY"] ?? "d5f0325a9dfb9517d0af48e4ca027a18";
            _city = configuration["CITY"] ?? "Karacasu,TR";
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = $"http://api.openweathermap.org/data/2.5/weather?q={_city}&appid={_apiKey}&units=metric";
                
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
