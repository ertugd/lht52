using Newtonsoft.Json;

namespace istiklal_karacasu_lorawan.Models
{
    // 1. Firebase'e Kaydedilecek Temiz Model
    public class GPSModel
    {
        [JsonProperty("lat")]
        public double Latitude { get; set; }

        [JsonProperty("lng")]
        public double Longitude { get; set; }

        [JsonProperty("hiz")]
        public double Hiz { get; set; } = 0; // SenseCAP genelde hız vermez, varsayılan 0

        [JsonProperty("pil")]
        public int Battery { get; set; }

        [JsonProperty("sonGuncelleme")]
        public string LastUpdate { get; set; }

        [JsonProperty("cihazAdi")]
        public string DeviceName { get; set; }
    }

    // 2. ChirpStack'ten Gelen Karmaşık JSON Yapısı
    public class ChirpstackIncomingDto
    {
        [JsonProperty("deviceInfo")]
        public DeviceInfo DeviceInfo { get; set; }

        // SenseCAP Decoder çıktısı buraya düşer
        [JsonProperty("object")]
        public SenseCapDecodedData Object { get; set; }
    }

    public class DeviceInfo
    {
        [JsonProperty("devEui")]
        public string DevEui { get; set; }

        [JsonProperty("deviceName")]
        public string DeviceName { get; set; }
    }

    public class SenseCapDecodedData
    {
        // ChirpStack codec'ine göre bu isimler değişebilir (lat/latitude)
        public double? latitude { get; set; }
        public double? longitude { get; set; }
        public int? battery { get; set; }
    }
}
