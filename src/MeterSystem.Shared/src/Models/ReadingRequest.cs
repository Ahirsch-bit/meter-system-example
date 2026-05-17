using System.Text.Json.Serialization;

namespace MeterSystem.Shared.Models;

public class ReadingRequest
{
    [JsonPropertyName("meter_number")]
    public int MeterNumber { get; set; }
    public Dictionary<DateTime, double> Readings { get; set; }
}
