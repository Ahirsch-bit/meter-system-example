namespace MeterSystem.Shared.Models;

public class ReadingMessage
{
    public long MeterNumber { get; set; }
    public required IReadOnlyCollection<MeterReadingMessage> Readings { get; set; }
}

public class MeterReadingMessage
{
    public DateTimeOffset ValueAt { get; set; }
    public decimal Value { get; set; }
}

