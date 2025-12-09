using System.Text.Json;

namespace istiklal_karacasu_lorawan.Services
{
    public class WeatherData
    {
        public double Temperature { get; set; }
        public double FeelsLike { get; set; }
        public int Humidity { get; set; }
        public double WindSpeed { get; set; }
        public int Pressure { get; set; }
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "";
        public string CityName { get; set; } = "";
        public DateTime DateTime { get; set; }
    }

    public class HourlyForecast
    {
        public DateTime DateTime { get; set; }
        public double Temperature { get; set; }
        public string Icon { get; set; } = "";
    }

    public class DailyForecast
    {
        public DateTime Date { get; set; }
        public double TempMin { get; set; }
        public double TempMax { get; set; }
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "";
        public int Humidity { get; set; }
        public double WindSpeed { get; set; }
    }

    public class WeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey = "d5f0325a9dfb9517d0af48e4ca027a18";
        private readonly string _city = "Kahramanmaras,TR";

        public WeatherService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<WeatherData? > GetCurrentWeatherAsync()
        {
            try
            {
                var url = $"https://api.openweathermap.org/data/2.5/weather?q={_city}&appid={_apiKey}&units=metric&lang=tr";
                var response = await _httpClient.GetStringAsync(url);
                var json = JsonDocument.Parse(response);
                var root = json.RootElement;

                return new WeatherData
                {
                    Temperature = root.GetProperty("main").GetProperty("temp").GetDouble(),
                    FeelsLike = root.GetProperty("main").GetProperty("feels_like").GetDouble(),
                    Humidity = root.GetProperty("main").GetProperty("humidity").GetInt32(),
                    WindSpeed = root.GetProperty("wind").GetProperty("speed").GetDouble(),
                    Pressure = root.GetProperty("main").GetProperty("pressure").GetInt32(),
                    Description = root.GetProperty("weather")[0].GetProperty("description").GetString() ?? "",
                    Icon = root.GetProperty("weather")[0].GetProperty("icon").GetString() ?? "",
                    CityName = root.GetProperty("name").GetString() ?? "",
                    DateTime = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Weather API Error: {ex.Message}");
                return null;
            }
        }

        public async Task<List<HourlyForecast>> GetHourlyForecastAsync()
        {
            try
            {
                var url = $"https://api.openweathermap.org/data/2.5/forecast?q={_city}&appid={_apiKey}&units=metric&lang=tr";
                var response = await _httpClient.GetStringAsync(url);
                var json = JsonDocument.Parse(response);
                var list = json.RootElement.GetProperty("list");

                var hourlyData = new List<HourlyForecast>();
                for (int i = 0; i < Math.Min(8, list.GetArrayLength()); i++) // 24 saat (8 x 3 saat)
                {
                    var item = list[i];
                    hourlyData.Add(new HourlyForecast
                    {
                        DateTime = DateTimeOffset.FromUnixTimeSeconds(item.GetProperty("dt").GetInt64()).DateTime,
                        Temperature = item.GetProperty("main").GetProperty("temp").GetDouble(),
                        Icon = item.GetProperty("weather")[0].GetProperty("icon").GetString() ?? ""
                    });
                }
                return hourlyData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hourly Forecast API Error: {ex.Message}");
                return new List<HourlyForecast>();
            }
        }

        public async Task<List<DailyForecast>> GetDailyForecastAsync()
        {
            try
            {
                var url = $"https://api.openweathermap.org/data/2.5/forecast?q={_city}&appid={_apiKey}&units=metric&lang=tr";
                var response = await _httpClient.GetStringAsync(url);
                var json = JsonDocument.Parse(response);
                var list = json.RootElement.GetProperty("list");

                var dailyData = new Dictionary<string, DailyForecast>();
                
                foreach (var item in list.EnumerateArray())
                {
                    var dt = DateTimeOffset.FromUnixTimeSeconds(item.GetProperty("dt").GetInt64()).DateTime;
                    var dateKey = dt.ToString("yyyy-MM-dd");
                    
                    if (! dailyData.ContainsKey(dateKey))
                    {
                        dailyData[dateKey] = new DailyForecast
                        {
                            Date = dt,
                            TempMin = item.GetProperty("main").GetProperty("temp_min").GetDouble(),
                            TempMax = item.GetProperty("main").GetProperty("temp_max").GetDouble(),
                            Description = item.GetProperty("weather")[0].GetProperty("description").GetString() ?? "",
                            Icon = item.GetProperty("weather")[0].GetProperty("icon").GetString() ?? "",
                            Humidity = item.GetProperty("main").GetProperty("humidity").GetInt32(),
                            WindSpeed = item.GetProperty("wind").GetProperty("speed").GetDouble()
                        };
                    }
                    else
                    {
                        var temp = item.GetProperty("main").GetProperty("temp").GetDouble();
                        if (temp < dailyData[dateKey].TempMin) dailyData[dateKey].TempMin = temp;
                        if (temp > dailyData[dateKey].TempMax) dailyData[dateKey].TempMax = temp;
                    }
                }

                return dailyData.Values.Take(5).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Daily Forecast API Error:  {ex.Message}");
                return new List<DailyForecast>();
            }
        }
    }
}