using MeterSystem.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace MeterSystem.Api.Controllers;

[ApiController]
[Route("api/")]
public class ReadingsController:ControllerBase
{
    private readonly IReadingService _readingService;

    public ReadingsController(IReadingService readingService)
    {
        _readingService = readingService;
    }

    [HttpPost]
    [Route("readings")]
    public async Task<IActionResult> AddReading([FromBody] Models.ReadingRequest readingRequest)
    {
        await _readingService.AddReading(readingRequest);
        return Accepted("readings");
    }
}
