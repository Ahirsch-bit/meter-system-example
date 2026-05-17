using MeterSystem.Api.Services;
using MeterSystem.Shared.Models;
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
    public async Task<IActionResult> AddReading([FromBody] ReadingRequest readingRequest)
    {
        await _readingService.AddReading(readingRequest);
        return Accepted("readings");
    }

    [HttpPost("readings/raw")]
    public async Task<IActionResult> PostRawAsync(
        [FromBody] RawReadingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _readingService.AcceptRawAsync(request, cancellationToken);

        return result ? Accepted() : BadRequest();
    }
}
