using Newtonsoft.Json;
using System; // DateTime için eklendi
using System.Collections.Generic;

namespace istiklal_karacasu_lorawan.Models
{
    // 1. Firebase'e Kaydedilecek Temiz Model (Burası Aynı Kalır)
    public class GPSModel
    {
        [JsonProperty("lat")]
        public double Latitude { get; set; }

        [JsonProperty("lng")]
        public double Longitude { get; set; }

        [JsonProperty("speed")]
        public double Hiz { get; set; } = 0;

        [JsonProperty("battery")]
        public int Battery { get; set; }

        [JsonProperty("last_update")]
        public string LastUpdate { get; set; }

        [JsonProperty("device_name")]
        public string DeviceName { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("is_tracking")]
        public bool IsTracking { get; set; } = false;
    }

    // 2. ChirpStack'ten Gelen Ana DTO
    public class ChirpstackIncomingDto
    {
        // YENİ EKLENEN: JSON'daki "time" bilgisini yakalar
        [JsonProperty("time")]
        public DateTime Time { get; set; }

        [JsonProperty("deviceInfo")]
        public DeviceInfo DeviceInfo { get; set; }

        [JsonProperty("object")]
        public SenseCapRawData Object { get; set; }
    }

    public class DeviceInfo
    {
        [JsonProperty("devEui")]
        public string DevEui { get; set; }

        [JsonProperty("deviceName")]
        public string DeviceName { get; set; }
    }

    // 3. JSON'daki "object" içindeki karmaşık yapıyı çözen sınıflar
    public class SenseCapRawData
    {
        [JsonProperty("messages")]
        public List<List<SenseCapMessageItem>> Messages { get; set; }
    }

    public class SenseCapMessageItem
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("measurementValue")]
        public object MeasurementValue { get; set; }
    }
}