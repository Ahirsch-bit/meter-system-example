using MeterSystem.Api.Models;

namespace MeterSystem.Api.Services;

public class ReadingService:IReadingService
{
    public Task AddReading(ReadingRequest readingRequest)
    {
        var message = new ReadingMessage()
        {
            MeterNumber = readingRequest.MeterNumber,
            Readings = readingRequest.Readings.Select(x => new MeterReadingMessage()
            {
                ValueAt = x.Key, Value = (decimal)x.Value
            }).ToList()
        };
        //TODO: Add Queue Logic
        return Task.CompletedTask;
    }
}
