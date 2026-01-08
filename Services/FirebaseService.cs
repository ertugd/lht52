using Firebase.Database;
using Firebase.Database.Query;
using istiklal_karacasu_lorawan.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace istiklal_karacasu_lorawan.Services
{
    public class FirebaseService
    {
        private readonly FirebaseClient _client;
        public string BaseUrl { get; }
        public string Secret { get; }

        public FirebaseService(IConfiguration config)
        {
            BaseUrl = config["Firebase:DatabaseUrl"];
            Secret = config["Firebase:DatabaseSecret"];

            _client = new FirebaseClient(
                BaseUrl,
                new FirebaseOptions { AuthTokenAsyncFactory = () => Task.FromResult(Secret) }
            );
        }

        public async Task<List<TelemetryEntry>> GetEntriesAsync(DateTime from, DateTime to)
        {
            var data = await _client
                .Child("telemetry")
                .OnceAsync<TelemetryEntry>();

            var filtered = data
                .Select(d => d.Object)
                .Where(e => e != null && e.Time >= from && e.Time <= to)
                .OrderBy(e => e.Time)
                .ToList();

            return filtered;
        }

        public async Task AddEntryTelemetryAsync(TelemetryEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            await _client
                .Child("telemetry")
                .PostAsync(entry);
        }

        // YENİ METOT: Mevcut GPS verisini (takip durumu dahil) okur
        public async Task<GPSModel> GetGPSAsync(string id)
        {
            try
            {
                // locations/{id} altındaki objeyi çeker
                return await _client
                    .Child("locations")
                    .Child(id)
                    .OnceSingleAsync<GPSModel>();
            }
            catch
            {
                return null;
            }
        }

        public async Task AddEntryGPSAsync(GPSModel entry, string id)
        {
            // 1. Ana Tabloyu Güncelle (Mevcut Konum)
            await _client
                .Child("locations")
                .Child(id)
                .PutAsync(entry);

            // 2. Eğer Takip Modu Açıksa, Geçmişe Kayıt At
            if (entry.IsTracking)
            {
                await _client
                    .Child("location_history") // Geçmiş verilerini ayrı tutuyoruz (overwrite riskine karşı)
                    .Child(id)
                    .PostAsync(entry);
            }
        }
    }
}