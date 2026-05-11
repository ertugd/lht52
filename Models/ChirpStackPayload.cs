using Newtonsoft.Json;
using System.Collections.Generic;

namespace istiklal_karacasu_lorawan.Models
{
    public class ChirpStackPayload
    {
        [JsonProperty("deduplicationId")]
        public string DeduplicationId { get; set; }

        [JsonProperty("time")]
        public string Time { get; set; }

        [JsonProperty("deviceInfo")]
        public DeviceInfo DeviceInfo { get; set; }

        [JsonProperty("object")]
        public dynamic Object { get; set; }
    }

    public class DeviceInfo
    {
        [JsonProperty("deviceName")]
        public string DeviceName { get; set; }

        [JsonProperty("devEui")]
        public string DevEui { get; set; }
    }

    // Helper classes for parsing the dynamic 'object'
    public class Lht52Payload
    {
        [JsonProperty("TempC_SHT")]
        public double? TempInner { get; set; }

        [JsonProperty("TempC_DS")]
        public double? TempOuter { get; set; }

        [JsonProperty("Hum_SHT")]
        public double? Humidity { get; set; }

        [JsonProperty("Battery")]
        public double? Battery { get; set; }
    }

    public class T1000BPayload
    {
        [JsonProperty("messages")]
        public List<List<T1000BMessage>> Messages { get; set; }
    }

    public class T1000BMessage
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("measurementValue")]
        public object MeasurementValue { get; set; }
    }
}
