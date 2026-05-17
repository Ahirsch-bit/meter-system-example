using MeterSystem.Shared.Models;

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
        return Task.CompletedTask;
    }

    public async Task<bool> AcceptRawAsync(
        RawReadingRequest request,
        CancellationToken cancellationToken)
    {
        if (request.MeterNumber <= 0)
            return false;

        if (string.IsNullOrWhiteSpace(request.Data))
            return false;

        byte[] bytes;

        try
        {
            bytes = Convert.FromBase64String(request.Data);
        }
        catch
        {
            return false;
        }

        MeterData meterData;

        try
        {
            meterData = MeterData.Parser.ParseFrom(bytes);
        }
        catch
        {
            return false;
        }

        if (meterData.Readings.Count == 0)
            return false;

        var message = new ReadingMessage
        {
            MeterNumber = request.MeterNumber,
            Readings = meterData.Readings
                .Select(r => new MeterReadingMessage
                {
                    ValueAt = r.Timestamp.ToDateTimeOffset(),
                    Value = (decimal)r.Value
                })
                .ToList()
        };

        await _messagePublisher.PublishAsync(message, cancellationToken);

        return true;
    }
}
