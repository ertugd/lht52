
    using Firebase.Database;
    using Firebase.Database.Query;
    using Google.Apis.Auth.OAuth2;
    using istiklal_karacasu_lorawan.Models;
    using System.Text.Json;

namespace istiklal_karacasu_lorawan.Services
{
    public class FirebaseService
    {
        private readonly FirebaseClient _client;

        public FirebaseService(IConfiguration config)
        {
            var databaseUrl = config["Firebase:DatabaseUrl"];
            var keyPath = config["Firebase:ServiceAccountKeyPath"];
            var databaseSecret = config["Firebase:DatabaseSecret"];

            GoogleCredential credential = null;
            if (!string.IsNullOrEmpty(keyPath) && File.Exists(keyPath))
            {
                credential = GoogleCredential.FromFile(keyPath);
            }

            _client = new FirebaseClient(
                databaseUrl,
                new FirebaseOptions
                {
                    AuthTokenAsyncFactory = () => Task.FromResult(databaseSecret)
                });
        }

        /// <summary>
        /// Firebase'e yeni bir TelemetryEntry nesnesi ekler.
        /// </summary>
        public async Task AddEntryAsync(TelemetryEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            // Id otomatik oluþturulmaz, bu nedenle Firebase için gerek yok.
            entry.Id = 0;

            // RawJson otomatik dolu deðilse, kendimiz doldururuz.
            if (string.IsNullOrEmpty(entry.RawJson))
                entry.RawJson = JsonSerializer.Serialize(entry);

            await _client
                .Child("telemetry")
                .PostAsync(entry);
        }

        public async Task<List<TelemetryEntry>> GetLast7DaysAsync()
        {
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            var filteredData = await _client
                .Child("telemetry")
                .OrderBy("Time")             // Firebase sorgusunda sýralama
                .StartAt(sevenDaysAgo)       // Sadece son 7 gün
                .OnceAsync<TelemetryEntry>();

            return filteredData
                .Select(x => x.Object)
                .OrderBy(x => x.Time)
                .ToList();
        }

        /// Verilen tarih aralýðýndaki TelemetryEntry kayýtlarýný getirir.
        public async Task<List<TelemetryEntry>> GetEntriesAsync(DateTime from, DateTime to)
        {
            var data = await _client
                .Child("telemetry")
                .OnceAsync<TelemetryEntry>();

            var list = new List<TelemetryEntry>();

            foreach (var item in data)
            {
                if (item.Object == null)
                    continue;

                var e = item.Object;
                if (e.Time >= from && e.Time <= to)
                {
                    list.Add(e);
                }
            }

            return list.OrderBy(e => e.Time).ToList();
        }

        /// <summary>
        /// Tüm kayýtlarý getirir (dikkat: büyük verilerde yavaþ olabilir)
        /// </summary>
        public async Task<List<TelemetryEntry>> GetAllAsync()
        {
            var data = await _client
                .Child("telemetry")
                .OnceAsync<TelemetryEntry>();

            return data.Select(d => d.Object)
                       .Where(e => e != null)
                       .OrderBy(e => e.Time)
                       .ToList()!;
        }
    }
}


