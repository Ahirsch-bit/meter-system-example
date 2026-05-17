using MeterSystem.Api.Models;

namespace MeterSystem.Api.Services;

public class ReadingService:IReadingService
{
    private readonly IMessagePublisher _messagePublisher;

    public ReadingService(IMessagePublisher messagePublisher)
    {
        _messagePublisher = messagePublisher;
    }
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
        _messagePublisher.PublishAsync(message, CancellationToken.None);
        //TODO: Replace the message publisher with actual Rabbit logic
        return Task.CompletedTask;
    }
}
