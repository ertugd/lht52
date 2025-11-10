using System.ComponentModel.DataAnnotations;

namespace istiklal_karacasu_lorawan.Models;

public class TelemetryEntry
{
    public DateTime Time { get; set; }
    public double? Temperature { get; set; }
    public double? Humidity { get; set; }
}