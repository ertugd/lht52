using System.ComponentModel.DataAnnotations;

namespace istiklal_karacasu_lorawan.Models;

public class TelemetryEntry
{
    [Key]
    public int Id { get; set; }
    public DateTime Time { get; set; }
    public double? Temperature { get; set; }
    public double? Humidity { get; set; }
    public string RawJson { get; set; } = null!;
}