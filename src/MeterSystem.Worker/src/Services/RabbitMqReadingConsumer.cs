using System.Text;
using System.Text.Json;
using MeterSystem.Shared.Configuration;
using MeterSystem.Shared.Models;
using MeterSystem.Worker.Repository;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MeterSystem.Worker.Services;

public class RabbitMqReadingConsumer:BackgroundService
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqReadingConsumer> _logger;
    private readonly IReadingsRepository _readingsRepository;

    public RabbitMqReadingConsumer(
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqReadingConsumer> logger,
        IReadingsRepository readingsRepository)
    {
        _options = options.Value;
        _logger = logger;
        _readingsRepository = readingsRepository;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password
        };

        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: _options.QueueName,
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

                var message = JsonSerializer.Deserialize<ReadingMessage>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                if (message is null)
                {
                    _logger.LogWarning("Received invalid reading message");
                    return;
                }
                await _readingsRepository.SaveAsync(message, stoppingToken);
                _logger.LogInformation(
                    "Received readings for meter {MeterNumber}. Count: {Count}",
                    message.MeterNumber,
                    message.Readings.Count);
                foreach (var reading in message.Readings)
                {
                    _logger.LogInformation(
                        "Meter {MeterNumber}: {ValueAt} = {Value}",
                        message.MeterNumber,
                        reading.ValueAt,
                        reading.Value);
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing reading message");
            }
        };
        await channel.BasicConsumeAsync(
            queue: _options.QueueName,
            autoAck: true,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation(
            "RabbitMQ consumer started. Queue: {QueueName}",
            _options.QueueName);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
