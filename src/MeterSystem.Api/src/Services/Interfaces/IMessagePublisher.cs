using MeterSystem.Api.Models;

namespace MeterSystem.Api.Services;

public interface IMessagePublisher
{
    Task PublishAsync(ReadingMessage message, CancellationToken cancellationToken);
}
