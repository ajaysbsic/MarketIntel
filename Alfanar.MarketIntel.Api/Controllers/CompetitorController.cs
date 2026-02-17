using Alfanar.MarketIntel.Application.DTOs;
using Alfanar.MarketIntel.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Alfanar.MarketIntel.Api.Controllers;

[ApiController]
[Route("api/competitors")]
public class CompetitorController : ControllerBase
{
    private readonly ICompetitorTrackingService _service;
    private readonly ILogger<CompetitorController> _logger;

    public CompetitorController(ICompetitorTrackingService service, ILogger<CompetitorController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<CompetitorDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = true)
    {
        var result = await _service.GetCompetitorsAsync(includeInactive);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("auto-detected")]
    [ProducesResponseType(typeof(List<CompetitorDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAutoDetected()
    {
        var result = await _service.GetAutoDetectedCompetitorsAsync();
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CompetitorDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateCompetitorDto dto)
    {
        var result = await _service.AddCompetitorAsync(dto);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return CreatedAtAction(nameof(GetDashboard), new { id = result.Data!.Id }, result.Data);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CompetitorDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateCompetitorDto dto)
    {
        var result = await _service.UpdateCompetitorAsync(id, dto);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _service.DeleteCompetitorAsync(id);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(new { message = "Competitor deleted" });
    }

    [HttpGet("{id:guid}/dashboard")]
    [ProducesResponseType(typeof(CompetitorDashboardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard(Guid id)
    {
        var result = await _service.GetCompetitorDashboardAsync(id);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("compare")]
    [ProducesResponseType(typeof(CompetitorComparisonDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Compare([FromBody] CompetitorCompareRequest request)
    {
        if (request.CompetitorIds == null || request.CompetitorIds.Count == 0)
            return BadRequest(new { message = "CompetitorIds are required" });

        var result = await _service.CompareCompetitorsAsync(request.CompetitorIds);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("{id:guid}/scan")]
    [ProducesResponseType(typeof(List<CompetitorMentionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ScanCompetitor(Guid id)
    {
        var result = await _service.ScanForMentionsAsync(id);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("scan-article")]
    [ProducesResponseType(typeof(List<CompetitorMentionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ScanArticle([FromBody] CompetitorScanRequestDto request)
    {
        var result = await _service.ScanArticleAsync(request);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }
}

public class CompetitorCompareRequest
{
    public List<Guid> CompetitorIds { get; set; } = new();
}
