using MeterSystem.Shared.Models;

namespace MeterSystem.Api.Services;

public interface IMessagePublisher
{
    Task PublishAsync(ReadingMessage message, CancellationToken cancellationToken);
}
