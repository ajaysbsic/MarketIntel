using Alfanar.MarketIntel.Application.DTOs;
using Alfanar.MarketIntel.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Alfanar.MarketIntel.Api.Controllers;

/// <summary>
/// API endpoints for web search operations and cached result retrieval
/// </summary>
[ApiController]
[Route("api/web-search")]
public class WebSearchController : ControllerBase
{
    private readonly IWebSearchService _service;
    private readonly ILogger<WebSearchController> _logger;

    public WebSearchController(
        IWebSearchService service,
        ILogger<WebSearchController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Performs a real-time web search and caches results
    /// </summary>
    [HttpPost("search")]
    [ProducesResponseType(typeof(List<WebSearchResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchAsync([FromBody] WebSearchRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        _logger.LogInformation("Processing web search for keyword: {Keyword}", request.Keyword);
        var result = await _service.SearchAsync(request);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    /// <summary>
    /// Retrieves cached search results with optional date range filtering and pagination
    /// </summary>
    [HttpGet("results")]
    [ProducesResponseType(typeof(PagedResultDto<WebSearchResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCachedResultsAsync(
        [FromQuery] string keyword,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return BadRequest(new { message = "Keyword cannot be empty" });

        _logger.LogInformation("Retrieving cached results for keyword: {Keyword}, page: {Page}", keyword, pageNumber);
        var result = await _service.GetCachedResultsAsync(keyword, fromDate, toDate, pageNumber, pageSize);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    /// <summary>
    /// Gets the total count of cached results for a keyword
    /// </summary>
    [HttpGet("results/count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetResultCountAsync([FromQuery] string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return BadRequest(new { message = "Keyword cannot be empty" });

        var result = await _service.GetResultCountAsync(keyword);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    /// <summary>
    /// Deduplicates cached results for a keyword (removes duplicate URLs)
    /// </summary>
    [HttpPost("results/deduplicate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeduplicateResultsAsync([FromQuery] string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return BadRequest(new { message = "Keyword cannot be empty" });

        _logger.LogInformation("Deduplicating cached results for keyword: {Keyword}", keyword);
        var result = await _service.DeduplicateResultsAsync(keyword);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(new { message = "Deduplication completed successfully" });
    }
}
