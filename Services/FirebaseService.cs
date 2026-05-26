using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
// Google.Apis.Auth artık kullanılmıyor, yerine Secret (Gizli Anahtar) kullanıyoruz.

namespace IstiklalLorawanAPI.Services
{
    // Bu arayüz (interface), Firebase servisimizin yapabileceği işlerin (fonksiyonların) bir sözleşmesidir.
    public interface IFirebaseService
    {
        Task SaveTempHumAsync(string devEui, string name, double? tempInner, double? tempOuter, double? humidity, double? battery, string timestamp);
        Task SaveGpsAsync(string devEui, string name, double lat, double lng, double? battery, string timestamp);
        Task SetRecordingAsync(string devEui, bool enabled);
        Task SaveWeatherAsync(object weatherData);
    }

    // Gerçek veritabanı işlemlerini yaptığımız asıl sınıf
    public class FirebaseService : IFirebaseService
    {
        // Firebase'e bağlanmamızı sağlayan istemci (client)
        private readonly FirebaseClient _client;
        private readonly bool _enableWeatherHistory;

        // Sınıf başlatıldığında ayarları yükleyen ve veritabanına bağlanan kısım
        public FirebaseService(IConfiguration configuration)
        {
            // Veritabanı internet adresini ayarlardan al, yoksa varsayılanı kullan
            string dbUrl = configuration["Firebase:DatabaseUrl"] ?? "https://istiklal-karacasu-default-rtdb.europe-west1.firebasedatabase.app";
            // Veritabanına güvenli giriş yapmak için gereken veritabanı şifresi (Auth Secret / Database Secret)
            string authSecret = configuration["Firebase:AuthSecret"] ?? configuration["Firebase:DatabaseSecret"];
            
            // Hava durumu geçmişi kaydetme özelliğinin aktif olup olmadığını kontrol et
            _enableWeatherHistory = configuration.GetValue<bool>("Firebase:EnableWeatherHistory", true);

            // Eğer şifre verilmemişse programı durdur ve hata ver
            if (string.IsNullOrEmpty(authSecret))
            {
                throw new ArgumentException("Firebase:AuthSecret veya Firebase:DatabaseSecret ayarı eksik. Lütfen Database Secret (Veritabanı Şifresi) girin.");
            }

            // Şifreyi kullanarak Firebase sunucusuna bağlan
            _client = new FirebaseClient(
                dbUrl,
                new FirebaseOptions
                {
                    AuthTokenAsyncFactory = () => Task.FromResult(authSecret)
                });
        }

        // --- ISI VE NEM VERİSİNİ KAYDETME ---
        public async Task SaveTempHumAsync(string devEui, string name, double? tempInner, double? tempOuter, double? humidity, double? battery, string timestamp)
        {
            // Veritabanında o cihaza ait klasörü (devices/cihaz_numarasi) bul
            var deviceRef = _client.Child("devices").Child(devEui);

            // Cihazın genel bilgilerini (isim, tip, son görülme saati vb.) güncelle
            var info = new
            {
                name = name,
                type = "temp-hum", // Isı ve Nem sensörü
                last_seen = timestamp,
                battery = battery ?? 100 // Batarya bilgisi yoksa %100 kabul et
            };
            // 'info' klasörünün içine bu bilgileri yama yap (sadece değişenleri güncelle)
            await deviceRef.Child("info").PatchAsync(info);

            // En son ölçülen değerleri paketle
            var data = new
            {
                temp_inner = tempInner,
                temp_outer = tempOuter,
                humidity = humidity,
                timestamp = timestamp
            };
            // Bu güncel ölçümleri 'latest' klasörüne yaz (Eski değeri tamamen ezer, böylece hep en son veriyi gösterir)
            await deviceRef.Child("latest").PutAsync(data);

            // Aynı ölçümleri bir de 'history' (geçmiş) listesine yeni bir kayıt olarak ekle (PostAsync alta yeni satır ekler)
            await deviceRef.Child("history").PostAsync(data);
        }

        // --- GPS (KONUM) VERİSİNİ KAYDETME ---
        public async Task SaveGpsAsync(string devEui, string name, double lat, double lng, double? battery, string timestamp)
        {
            // Cihaz klasörünü bul
            var deviceRef = _client.Child("devices").Child(devEui);

            // Cihaz bilgilerini güncelle
            var info = new
            {
                name = name,
                type = "gps", // GPS (Konum) takip cihazı
                last_seen = timestamp,
                battery = battery ?? 100
            };
            await deviceRef.Child("info").PatchAsync(info);

            // En son konum bilgisini 'latest' klasörüne yaz (Haritada anında o konuma zıplasın diye)
            var data = new
            {
                lat = lat, // Enlem
                lng = lng, // Boylam
                timestamp = timestamp
            };
            await deviceRef.Child("latest").PutAsync(data);

            // Acaba kullanıcı web sitesinden "Rotayı Kaydetmeye Başla" dedi mi diye ayarlara bak
            var settings = await deviceRef.Child("settings").OnceSingleAsync<IDictionary<string, object>>();
            
            // Eğer ayarlar varsa ve 'record_path' (yolu kaydet) özelliği True (Açık) ise
            if (settings != null && settings.ContainsKey("record_path") && Convert.ToBoolean(settings["record_path"]))
            {
                // Geçmiş kayıtları "oturum" (session) adı verilen parçalara bölerek kaydet
                // Oturum kimliği (session_id) verilmişse onu kullan, yoksa "default_session" klasörüne ekle
                string sessionId = settings.ContainsKey("current_session_id") ? settings["current_session_id"].ToString() : "default_session";
                await deviceRef.Child("history").Child(sessionId).PostAsync(data);
            }
        }

        // --- GPS KAYDINI AÇIP KAPATMA ---
        public async Task SetRecordingAsync(string devEui, bool enabled)
        {
            // Web sitesinden gelen komuta göre 'record_path' ayarını True veya False yap
            await _client.Child("devices").Child(devEui).Child("settings").PatchAsync(new
            {
                record_path = enabled
            });
        }

        // --- DIŞ ORTAM HAVA DURUMUNU KAYDETME ---
        public async Task SaveWeatherAsync(object weatherData)
        {
            // En son hava durumunu ana dizine yazdır
            await _client.Child("weather").PutAsync(weatherData);
            
            // Tarihsel analiz için geçmişe de bir kopyasını at (yapılandırmaya göre kontrol et)
            if (_enableWeatherHistory)
            {
                await _client.Child("weather_history").PostAsync(weatherData);
            }
        }
    }
}
