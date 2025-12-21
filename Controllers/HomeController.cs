using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json; // JSON kütüphanesi
using System.Threading.Tasks;

namespace lht52.Controllers // BURAYI KENDİ PROJE ADINLA DEĞİŞTİR
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetWeatherData()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    // Open-Meteo API: Kahramanmaraş Koordinatları (37.58, 36.93)
                    // API Key gerekmez, ücretsizdir.
                    string url = "https://api.open-meteo.com/v1/forecast?latitude=37.5858&longitude=36.9371&current=temperature_2m,relative_humidity_2m,surface_pressure,wind_speed_10m,weather_code&hourly=temperature_2m,weather_code&daily=weather_code,temperature_2m_max,temperature_2m_min&timezone=auto";

                    var response = await client.GetStringAsync(url);
                    
                    // Gelen JSON verisini dinamik olarak işle
                    using (JsonDocument doc = JsonDocument.Parse(response))
                    {
                        var root = doc.RootElement;
                        var current = root.GetProperty("current");
                        var daily = root.GetProperty("daily");
                        var hourly = root.GetProperty("hourly");

                        // ANLIK VERİLER
                        double temp = current.GetProperty("temperature_2m").GetDouble();
                        int humidity = current.GetProperty("relative_humidity_2m").GetInt32();
                        double windKmh = current.GetProperty("wind_speed_10m").GetDouble();
                        double pressure = current.GetProperty("surface_pressure").GetDouble();
                        int wmoCode = current.GetProperty("weather_code").GetInt32();

                        // JS tarafı rüzgarı m/s bekliyor (3.6 ile çarpıyor), o yüzden burada bölüyoruz
                        double windMs = windKmh / 3.6;

                        // WMO Kodunu Açıklamaya ve İkona Çevir
                        var (condition, iconCode) = ParseWmoCode(wmoCode);

                        // SAATLİK VERİLER
                        var hourlyData = new List<object>();
                        var hourlyTemps = hourly.GetProperty("temperature_2m");
                        var hourlyTimes = hourly.GetProperty("time");
                        // Şu anki saatten itibaren sonraki 6 saati al
                        int currentHourIndex = DateTime.Now.Hour; 
                        
                        for (int i = 0; i < 6; i++)
                        {
                            // Dizi taşmasın diye kontrol
                            int index = currentHourIndex + i;
                            if (index < hourlyTemps.GetArrayLength())
                            {
                                hourlyData.Add(new { 
                                    dateTime = DateTime.Now.AddHours(i), 
                                    temperature = hourlyTemps[index].GetDouble(), 
                                    icon = iconCode 
                                });
                            }
                        }

                        // GÜNLÜK VERİLER
                        var dailyData = new List<object>();
                        var dailyMax = daily.GetProperty("temperature_2m_max");
                        var dailyMin = daily.GetProperty("temperature_2m_min");
                        var dailyCodes = daily.GetProperty("weather_code");
                        var dailyDates = daily.GetProperty("time");

                        for (int i = 0; i < 5; i++)
                        {
                            int code = dailyCodes[i].GetInt32();
                            var (dailyDesc, dailyIcon) = ParseWmoCode(code);
                            
                            dailyData.Add(new { 
                               date = DateTime.Parse(dailyDates[i].GetString()!), 
                                tempMin = dailyMin[i].GetDouble(), 
                                tempMax = dailyMax[i].GetDouble(), 
                                description = dailyDesc, 
                                icon = dailyIcon, 
                                humidity = 50, // Günlük nem tahmini API'de yoksa ortalama veriyoruz
                                windSpeed = 5 
                            });
                        }

                        return Json(new
                        {
                            current = new
                            {
                                cityName = "Kahramanmaraş",
                                description = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(condition.ToLower()),
                                temperature = temp,
                                humidity = humidity,
                                windSpeed = windMs,
                                pressure = pressure,
                                // MGM'deki gibi Tarih Saat bilgisini buraya ekliyoruz
                                dateString = DateTime.Now.ToString("dd MMMM - HH.mm", new CultureInfo("tr-TR"))
                            },
                            hourly = hourlyData,
                            daily = dailyData
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // Hava Durumu Kodlarını Türkçeye ve İkona Çeviren Yardımcı Metot
        private (string, string) ParseWmoCode(int code)
        {
            // 0: Açık, 1-3: Bulutlu, 45-48: Sis, 51-67: Yağmur, 71-77: Kar, 95: Fırtına
            if (code == 0) return ("Açık", "01d");
            if (code == 1) return ("Az Bulutlu", "02d");
            if (code == 2) return ("Parçalı Bulutlu", "02d");
            if (code == 3) return ("Kapalı", "04d");
            if (code >= 45 && code <= 48) return ("Sisli", "50d");
            if (code >= 51 && code <= 67) return ("Yağmurlu", "09d");
            if (code >= 71 && code <= 77) return ("Karlı", "13d");
            if (code >= 80 && code <= 82) return ("Sağanak Yağış", "09d");
            if (code >= 95) return ("Fırtına", "11d");
            
            return ("Bulutlu", "02d");
        }
    }
}