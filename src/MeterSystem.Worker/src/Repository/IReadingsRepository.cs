using MeterSystem.Shared.Models;

namespace MeterSystem.Worker.Repository;

public interface IReadingsRepository
{
    Task SaveAsync(ReadingMessage message, CancellationToken cancellationToken);
}
