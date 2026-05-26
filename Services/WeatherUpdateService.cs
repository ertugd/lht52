using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using IstiklalLorawanAPI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace IstiklalLorawanAPI.Services
{
    public class WeatherUpdateService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<WeatherUpdateService> _logger;
        private readonly string _apiKey;
        private readonly string _city;
        private readonly int _intervalMinutes;

        public WeatherUpdateService(
            IServiceProvider serviceProvider,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<WeatherUpdateService> logger)
        {
            _serviceProvider = serviceProvider;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _apiKey = configuration["WEATHER_API_KEY"] ?? "d5f0325a9dfb9517d0af48e4ca027a18";
            _city = configuration["CITY"] ?? "Karacasu,TR";
            _intervalMinutes = configuration.GetValue<int>("WeatherIntervalMinutes", 10);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Weather Update Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await UpdateWeatherAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while updating weather.");
                }

                await Task.Delay(TimeSpan.FromMinutes(_intervalMinutes), stoppingToken);
            }

            _logger.LogInformation("Weather Update Service is stopping.");
        }

        private async Task UpdateWeatherAsync()
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"http://api.openweathermap.org/data/2.5/weather?q={_city}&appid={_apiKey}&units=metric&lang=tr";

            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var weatherData = JsonConvert.DeserializeObject<dynamic>(content);

                // Option B: Advanced Data Set
                var filteredData = new
                {
                    main = new
                    {
                        temp = weatherData.main.temp,
                        feels_like = weatherData.main.feels_like,
                        humidity = weatherData.main.humidity,
                        pressure = weatherData.main.pressure
                    },
                    wind = new
                    {
                        speed = weatherData.wind.speed
                    },
                    visibility = weatherData.visibility,
                    weather = weatherData.weather,
                    last_updated = DateTime.UtcNow.ToString("o")
                };

                using (var scope = _serviceProvider.CreateScope())
                {
                    var firebaseService = scope.ServiceProvider.GetRequiredService<IFirebaseService>();
                    await firebaseService.SaveWeatherAsync(filteredData);
                    _logger.LogInformation("Weather data updated in Firebase successfully.");
                }
            }
            else
            {
                _logger.LogWarning($"Failed to fetch weather: {response.StatusCode}");
            }
        }
    }
}
