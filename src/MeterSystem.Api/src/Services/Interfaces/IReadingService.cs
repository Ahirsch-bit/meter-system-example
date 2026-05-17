using MeterSystem.Api.Models;

namespace MeterSystem.Api.Services;

public interface IReadingService
{
    Task AddReading(ReadingRequest readingRequest);
}
