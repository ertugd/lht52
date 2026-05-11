using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace istiklal_karacasu_lorawan.Models
{
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

        // YENİ EKLENEN: Takip Oturumu ID'si
        [JsonProperty("session_id")]
        public string SessionId { get; set; }
    }

}