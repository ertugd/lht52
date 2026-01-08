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

        public async Task<GPSModel> GetGPSAsync(string id)
        {
            try
            {
                return await _client.Child("locations").Child(id).OnceSingleAsync<GPSModel>();
            }
            catch
            {
                return null;
            }
        }

        public async Task AddEntryGPSAsync(GPSModel entry, string id)
        {
            await _client.Child("locations").Child(id).PutAsync(entry);

            if (entry.IsTracking)
            {
                await _client.Child("location_history").Child(id).PostAsync(entry);
            }
        }

        // --- GÜNCELLENEN METOT ---
        public async Task UpdateTrackingAsync(string id, bool status, string sessionId)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            // 1. Takip Durumunu Güncelle
            await _client
                .Child("locations")
                .Child(id)
                .Child("is_tracking")
                .PutAsync(status);

            // 2. Session ID ve Hız Yönetimi
            var locationNode = _client.Child("locations").Child(id);

            if (status && !string.IsNullOrEmpty(sessionId))
            {
                // Takip başladığında:
                // A. Yeni Session ID'yi yaz (Tırnaklı string olarak)
                string jsonString = "\"" + sessionId + "\"";
                await locationNode.Child("session_id").PutAsync(jsonString);

                // B. HIZI SIFIRLA (İstediğiniz Özellik)
                // Takip başladığı an eski hız verisi silinir ve 0 yapılır.
                await locationNode.Child("speed").PutAsync(0);
            }
            else
            {
                // Takip bittiyse SessionID'yi sil
                await locationNode.Child("session_id").DeleteAsync();

                // İsteğe bağlı: Takip bitince de hızı sıfırlamak isterseniz burayı açabilirsiniz:
                 await locationNode.Child("speed").PutAsync(0);
            }
        }
    }
}