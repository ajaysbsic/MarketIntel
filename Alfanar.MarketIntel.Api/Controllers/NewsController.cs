using Alfanar.MarketIntel.Api.Hubs;
using Alfanar.MarketIntel.Application.DTOs;
using Alfanar.MarketIntel.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Alfanar.MarketIntel.Api.Controllers;

[ApiController]
[Route("api/news")]
public class NewsController : ControllerBase
{
    private readonly INewsService _newsService;
    private readonly IHubContext<NotificationsHub> _hub;
    private readonly IValidator<IngestNewsRequest> _validator;
    private readonly ILogger<NewsController> _logger;

    public NewsController(
        INewsService newsService,
        IHubContext<NotificationsHub> hub,
        IValidator<IngestNewsRequest> validator,
        ILogger<NewsController> logger)
    {
        _newsService = newsService;
        _hub = hub;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>
    /// Ingest a new news article from RSS feed or manual entry
    /// </summary>
    [HttpPost("ingest")]
    [ProducesResponseType(typeof(NewsArticleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Ingest([FromBody] IngestNewsRequest request)
    {
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(new { errors });
        }

        var result = await _newsService.IngestArticleAsync(request);
        
        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("already exists") == true)
                return Conflict(new { message = result.Error });
            
            return BadRequest(new { message = result.Error });
        }

        // Send real-time notification to connected clients
        await _hub.Clients.All.SendAsync("newArticle", new
        {
            result.Data!.Id,
            result.Data.Title,
            result.Data.Url,
            result.Data.Category,
            result.Data.Region,
            result.Data.PublishedUtc,
            result.Data.Summary
        });

        return Ok(result.Data);
    }

    /// <summary>
    /// Get a specific news article by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(NewsArticleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _newsService.GetByIdAsync(id);
        
        if (!result.IsSuccess)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    /// <summary>
    /// Get filtered and paginated news articles
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFiltered([FromQuery] NewsFilterDto filter)
    {
        var result = await _newsService.GetFilteredArticlesAsync(filter);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(new
        {
            items = result.Data!.Items,
            pageNumber = result.Data.PageNumber,
            pageSize = result.Data.PageSize,
            totalPages = result.Data.TotalPages,
            totalCount = result.Data.TotalCount,
            hasPreviousPage = result.Data.HasPreviousPage,
            hasNextPage = result.Data.HasNextPage
        });
    }

    /// <summary>
    /// Get most recent news articles
    /// </summary>
    [HttpGet("recent")]
    [ProducesResponseType(typeof(List<NewsArticleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecent([FromQuery] int count = 10)
    {
        if (count < 1 || count > 100)
            return BadRequest(new { message = "Count must be between 1 and 100" });

        var result = await _newsService.GetRecentArticlesAsync(count);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    /// <summary>
    /// Delete a news article
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _newsService.DeleteArticleAsync(id);
        
        if (!result.IsSuccess)
            return NotFound(new { message = result.Error });

        return NoContent();
    }

    /// <summary>
    /// Get all distinct categories
    /// </summary>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories()
    {
        var result = await _newsService.GetAllCategoriesAsync();
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    /// <summary>
    /// Get all distinct regions
    /// </summary>
    [HttpGet("regions")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRegions()
    {
        var result = await _newsService.GetAllRegionsAsync();
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }
}