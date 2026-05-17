using MeterSystem.Shared.Models;

namespace MeterSystem.Api.Services;

public interface IReadingService
{
    Task AddReading(ReadingRequest readingRequest);
    Task<bool> AcceptRawAsync(RawReadingRequest request, CancellationToken cancellationToken);
}

