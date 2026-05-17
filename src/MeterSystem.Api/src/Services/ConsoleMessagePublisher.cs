using MeterSystem.Api.Models;

namespace MeterSystem.Api.Services;

public class ConsoleMessagePublisher : IMessagePublisher
{
    //Placeholder class
    public Task PublishAsync(ReadingMessage message, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Meter Number: {message.MeterNumber}");
        foreach (var reading in message.Readings)
        {
            Console.WriteLine($"Value At: {reading.ValueAt}, Value: {reading.Value}");
        }
        return Task.CompletedTask;
    }
}
