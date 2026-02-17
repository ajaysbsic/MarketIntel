using Alfanar.MarketIntel.Application.DTOs;
using Alfanar.MarketIntel.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Alfanar.MarketIntel.Api.Controllers;

[ApiController]
[Route("api/trends")]
public class TrendController : ControllerBase
{
    private readonly ITrendAnalyticsService _service;

    public TrendController(ITrendAnalyticsService service)
    {
        _service = service;
    }

    [HttpPost("generate-snapshot")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateSnapshot([FromBody] GenerateSnapshotRequestDto request)
    {
        var date = request.Date ?? DateTime.UtcNow.Date;
        var result = await _service.GenerateDailySnapshotAsync(date);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(new { message = "Snapshot generated", date });
    }

    [HttpGet("keyword/{keyword}")]
    [ProducesResponseType(typeof(List<TrendPointDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetKeywordTrend(string keyword, [FromQuery] int days = 30)
    {
        var result = await _service.GetKeywordTrendAsync(keyword, days);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("competitor/{id:guid}")]
    [ProducesResponseType(typeof(List<CompetitorVisibilityPointDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCompetitorVisibility(Guid id, [FromQuery] int days = 30)
    {
        var result = await _service.GetCompetitorVisibilityAsync(id, days);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("noise-vs-signal/{keyword}")]
    [ProducesResponseType(typeof(List<NoiseSignalPointDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNoiseVsSignal(string keyword, [FromQuery] int days = 30)
    {
        var result = await _service.GetMarketNoiseVsSignalAsync(keyword, days);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("compare")]
    [ProducesResponseType(typeof(TrendComparisonDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Compare([FromQuery] string keywords, [FromQuery] int days = 30)
    {
        var list = keywords.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        if (list.Count == 0)
            return BadRequest(new { message = "Keywords are required" });

        var result = await _service.GetTrendComparisonAsync(list, days);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("weekly-digest")]
    [ProducesResponseType(typeof(WeeklyDigestDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWeeklyDigest()
    {
        var result = await _service.GetWeeklyDigestAsync();
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }
}
