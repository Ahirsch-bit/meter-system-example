using System.Text.Json.Serialization;

namespace MeterSystem.Shared.Models;

public class RawReadingRequest
{
    [JsonPropertyName("meter_number")]
    public long MeterNumber { get; set; }

    [JsonPropertyName("data")]
    public string? Data { get; set; }
}
