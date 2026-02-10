using Alfanar.MarketIntel.Application.DTOs;
using Alfanar.MarketIntel.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Alfanar.MarketIntel.Api.Controllers;

[ApiController]
[Route("api/technology-intelligence")]
public class TechnologyIntelligenceController : ControllerBase
{
    private readonly ITechnologyIntelligenceService _service;
    private readonly ILogger<TechnologyIntelligenceController> _logger;

    public TechnologyIntelligenceController(
        ITechnologyIntelligenceService service,
        ILogger<TechnologyIntelligenceController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("overview")]
    [ProducesResponseType(typeof(TechnologyOverviewDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverview([FromQuery] TechnologyIntelligenceFilterDto filter)
    {
        var result = await _service.GetOverviewAsync(filter);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("timeline")]
    [ProducesResponseType(typeof(List<TechnologyTrendPointDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTimeline([FromQuery] TechnologyIntelligenceFilterDto filter)
    {
        var result = await _service.GetTimelineAsync(filter);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("regions")]
    [ProducesResponseType(typeof(List<TechnologyRegionSignalDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRegions([FromQuery] TechnologyIntelligenceFilterDto filter)
    {
        var result = await _service.GetRegionsAsync(filter);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("key-players")]
    [ProducesResponseType(typeof(List<TechnologyKeyPlayerDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetKeyPlayers([FromQuery] TechnologyIntelligenceFilterDto filter, [FromQuery] int maxItems = 10)
    {
        if (maxItems < 1 || maxItems > 50)
            return BadRequest(new { message = "maxItems must be between 1 and 50" });

        var result = await _service.GetKeyPlayersAsync(filter, maxItems);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("insights")]
    [ProducesResponseType(typeof(List<TechnologyInsightDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInsights([FromQuery] TechnologyIntelligenceFilterDto filter)
    {
        var result = await _service.GetInsightsAsync(filter);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(TechnologyIntelligenceSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary([FromQuery] TechnologyIntelligenceFilterDto filter)
    {
        var result = await _service.GetSummaryAsync(filter);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }
}
