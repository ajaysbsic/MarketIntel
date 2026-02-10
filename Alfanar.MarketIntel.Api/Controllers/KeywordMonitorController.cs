using Alfanar.MarketIntel.Application.DTOs;
using Alfanar.MarketIntel.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Alfanar.MarketIntel.Api.Controllers;

/// <summary>
/// API endpoints for managing keyword monitors with CRUD operations
/// </summary>
[ApiController]
[Route("api/keyword-monitors")]
public class KeywordMonitorController : ControllerBase
{
    private readonly IKeywordMonitorService _service;
    private readonly ILogger<KeywordMonitorController> _logger;

    public KeywordMonitorController(
        IKeywordMonitorService service,
        ILogger<KeywordMonitorController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new keyword monitor
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(KeywordMonitorDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateMonitorAsync([FromBody] CreateKeywordMonitorDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        _logger.LogInformation("Creating new keyword monitor for: {Keyword}", dto.Keyword);
        var result = await _service.CreateMonitorAsync(dto);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Created($"/api/keyword-monitors/{result.Data.Id}", result.Data);
    }

    /// <summary>
    /// Retrieves all keyword monitors with optional active filter
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<KeywordMonitorDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllMonitorsAsync([FromQuery] bool? activeOnly = false)
    {
        _logger.LogInformation("Retrieving keyword monitors (activeOnly: {ActiveOnly})", activeOnly);
        var result = await _service.GetAllMonitorsAsync();
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        var data = activeOnly == true 
            ? result.Data.Where(m => m.IsActive).ToList() 
            : result.Data;

        return Ok(data);
    }

    /// <summary>
    /// Retrieves a specific keyword monitor by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(KeywordMonitorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMonitorByIdAsync(Guid id)
    {
        var result = await _service.GetMonitorByIdAsync(id);
        
        if (!result.IsSuccess)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    /// <summary>
    /// Updates an existing keyword monitor
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(KeywordMonitorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMonitorAsync(Guid id, [FromBody] CreateKeywordMonitorDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        _logger.LogInformation("Updating keyword monitor {Id}", id);
        var result = await _service.UpdateMonitorAsync(id, dto);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    /// <summary>
    /// Deletes a keyword monitor
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMonitorAsync(Guid id)
    {
        _logger.LogInformation("Deleting keyword monitor {Id}", id);
        var result = await _service.DeleteMonitorAsync(id);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(new { message = "Monitor deleted successfully" });
    }

    /// <summary>
    /// Toggles a keyword monitor's active status
    /// </summary>
    [HttpPost("{id}/toggle")]
    [ProducesResponseType(typeof(KeywordMonitorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleMonitorAsync(Guid id, [FromQuery] bool isActive)
    {
        _logger.LogInformation("Toggling keyword monitor {Id} to {IsActive}", id, isActive);
        var result = await _service.ToggleMonitorAsync(id, isActive);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    /// <summary>
    /// Gets all keyword monitors that are due for checking based on their interval
    /// </summary>
    [HttpGet("due-for-check/list")]
    [ProducesResponseType(typeof(List<KeywordMonitorDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMonitorsDueForCheckAsync([FromQuery] int intervalMinutes = 60)
    {
        _logger.LogInformation("Retrieving monitors due for check with interval {Minutes} minutes", intervalMinutes);
        var result = await _service.GetMonitorsDueForCheckAsync(intervalMinutes);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    /// <summary>
    /// Gets monitors that are active (for backend watcher use)
    /// </summary>
    [HttpGet("active/list")]
    [ProducesResponseType(typeof(List<KeywordMonitorDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveMonitorsAsync()
    {
        var result = await _service.GetActiveMonitorsAsync();
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }
}
