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
    // Bu arayüz (interface), Firebase servisimizin yapabileceği işlerin (fonksiyonların) bir sözleşmesidir.
    public interface IFirebaseService
    {
        Task SaveTempHumAsync(string devEui, string name, double? tempInner, double? tempOuter, double? humidity, double? battery, string timestamp);
        Task SaveGpsAsync(string devEui, string name, double lat, double lng, double? battery, string timestamp);
        Task SetRecordingAsync(string devEui, bool enabled);
    }

    public class FirebaseService : IFirebaseService
    {
        private readonly FirebaseClient _client;
        public string BaseUrl { get; }
        public string Secret { get; }

        public FirebaseService(IConfiguration config)
        {
            BaseUrl = config["Firebase:DatabaseUrl"] ?? "https://istiklal-karacasu-default-rtdb.europe-west1.firebasedatabase.app";
            Secret = config["Firebase:DatabaseSecret"] ?? config["Firebase:AuthSecret"];

            if (string.IsNullOrEmpty(Secret))
            {
                throw new ArgumentException("Firebase:DatabaseSecret veya Firebase:AuthSecret ayarı eksik.");
            }

            _client = new FirebaseClient(
                BaseUrl,
                new FirebaseOptions { AuthTokenAsyncFactory = () => Task.FromResult(Secret) }
            );
        }

        // --- REFERANS PROJEDEN GELEN METOTLAR (YENİ YAPI) ---

        public async Task SaveTempHumAsync(string devEui, string name, double? tempInner, double? tempOuter, double? humidity, double? battery, string timestamp)
        {
            var deviceRef = _client.Child("devices").Child(devEui);
            var info = new
            {
                name = name,
                type = "temp-hum",
                last_seen = timestamp,
                battery = battery ?? 100
            };
            await deviceRef.Child("info").PatchAsync(info);

            var data = new
            {
                temp_inner = tempInner,
                temp_outer = tempOuter,
                humidity = humidity,
                timestamp = timestamp
            };
            await deviceRef.Child("latest").PutAsync(data);
            await deviceRef.Child("history").PostAsync(data);
        }

        public async Task SaveGpsAsync(string devEui, string name, double lat, double lng, double? battery, string timestamp)
        {
            var deviceRef = _client.Child("devices").Child(devEui);
            var info = new
            {
                name = name,
                type = "gps",
                last_seen = timestamp,
                battery = battery ?? 100
            };
            await deviceRef.Child("info").PatchAsync(info);

            var data = new
            {
                lat = lat,
                lng = lng,
                timestamp = timestamp
            };
            await deviceRef.Child("latest").PutAsync(data);

            var settings = await deviceRef.Child("settings").OnceSingleAsync<IDictionary<string, object>>();
            if (settings != null && settings.ContainsKey("record_path") && Convert.ToBoolean(settings["record_path"]))
            {
                string sessionId = settings.ContainsKey("current_session_id") ? settings["current_session_id"].ToString() : "default_session";
                await deviceRef.Child("history").Child(sessionId).PostAsync(data);
            }
        }

        public async Task SetRecordingAsync(string devEui, bool enabled)
        {
            await _client.Child("devices").Child(devEui).Child("settings").PatchAsync(new
            {
                record_path = enabled
            });
        }

        // --- MEVCUT METOTLAR (ESKİ PANEL UYUMLULUĞU İÇİN) ---

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

        public async Task UpdateTrackingAsync(string id, bool status, string sessionId)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            await _client
                .Child("locations")
                .Child(id)
                .Child("is_tracking")
                .PutAsync(status);

            var locationNode = _client.Child("locations").Child(id);

            if (status && !string.IsNullOrEmpty(sessionId))
            {
                string jsonString = "\"" + sessionId + "\"";
                await locationNode.Child("session_id").PutAsync(jsonString);
                await locationNode.Child("speed").PutAsync(0);
            }
            else
            {
                await locationNode.Child("session_id").DeleteAsync();
                await locationNode.Child("speed").PutAsync(0);
            }
        }
    }
}