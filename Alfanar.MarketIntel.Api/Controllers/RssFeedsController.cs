using Alfanar.MarketIntel.Application.DTOs;
using Alfanar.MarketIntel.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Alfanar.MarketIntel.Api.Controllers;

[ApiController]
[Route("api/feeds")]
public class RssFeedsController : ControllerBase
{
    private readonly IRssFeedService _feedService;
    private readonly ILogger<RssFeedsController> _logger;

    public RssFeedsController(IRssFeedService feedService, ILogger<RssFeedsController> logger)
    {
        _feedService = feedService;
        _logger = logger;
    }

    /// <summary>
    /// Get all RSS feeds
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<RssFeedDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _feedService.GetAllFeedsAsync();
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    /// <summary>
    /// Get all active RSS feeds
    /// </summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(List<RssFeedDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActive()
    {
        var result = await _feedService.GetActiveFeedsAsync();
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    /// <summary>
    /// Get a specific RSS feed by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RssFeedDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _feedService.GetFeedByIdAsync(id);
        
        if (!result.IsSuccess)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    /// <summary>
    /// Create a new RSS feed
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(RssFeedDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] RssFeedDto feedDto)
    {
        if (string.IsNullOrWhiteSpace(feedDto.Name) || string.IsNullOrWhiteSpace(feedDto.Url))
            return BadRequest(new { message = "Name and URL are required" });

        var result = await _feedService.CreateFeedAsync(feedDto);
        
        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("already exists") == true)
                return Conflict(new { message = result.Error });
            
            return BadRequest(new { message = result.Error });
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// Update an existing RSS feed
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] RssFeedDto feedDto)
    {
        if (string.IsNullOrWhiteSpace(feedDto.Name) || string.IsNullOrWhiteSpace(feedDto.Url))
            return BadRequest(new { message = "Name and URL are required" });

        var result = await _feedService.UpdateFeedAsync(id, feedDto);
        
        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("not found") == true)
                return NotFound(new { message = result.Error });
            
            return BadRequest(new { message = result.Error });
        }

        return NoContent();
    }

    /// <summary>
    /// Toggle RSS feed active status
    /// </summary>
    [HttpPatch("{id:guid}/toggle")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Toggle(Guid id)
    {
        var result = await _feedService.ToggleFeedStatusAsync(id);
        
        if (!result.IsSuccess)
            return NotFound(new { message = result.Error });

        return NoContent();
    }

    /// <summary>
    /// Delete an RSS feed
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _feedService.DeleteFeedAsync(id);
        
        if (!result.IsSuccess)
            return NotFound(new { message = result.Error });

        return NoContent();
    }

    /// <summary>
    /// Update feed last fetched timestamp (called by Python watcher)
    /// </summary>
    [HttpPost("{id:guid}/update-fetch")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLastFetched(Guid id, [FromQuery] string? etag = null)
    {
        var result = await _feedService.UpdateFeedLastFetchedAsync(id, etag);
        
        if (!result.IsSuccess)
            return NotFound(new { message = result.Error });

        return NoContent();
    }
}
