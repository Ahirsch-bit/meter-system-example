using MeterSystem.Api.Models;

namespace MeterSystem.Worker.Repository;

public interface IReadingsRepository
{
    Task SaveAsync(ReadingMessage message, CancellationToken cancellationToken);
}
