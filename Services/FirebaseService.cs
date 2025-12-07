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

            // Sadece istenen tarih aralığındaki verileri filtrele
            var filtered = data
                .Select(d => d.Object)
                .Where(e => e != null && e.Time >= from && e.Time <= to)
                .OrderBy(e => e.Time)
                .ToList();

            return filtered;
        }

        /// Firebase'e yeni bir TelemetryEntry nesnesi ekler.
        public async Task AddEntryTelemetryAsync(TelemetryEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));  

            await _client
                .Child("telemetry")
                .PostAsync(entry);
        }

        public async Task AddEntryGPSAsync(GPSModel entry,string id)
        {                  
                await _client
                    .Child("locations")                // Ana Tablo
                    .Child(id)    // Cihaz ID (Benzersiz Key)
                    .PutAsync(entry);                  // GpsModel'i gönder                         
        }
    }
}
