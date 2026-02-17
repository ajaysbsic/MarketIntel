using System.Security.Claims;
using Alfanar.MarketIntel.Application.DTOs;
using Alfanar.MarketIntel.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alfanar.MarketIntel.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationPreferencesController : ControllerBase
{
    private readonly INotificationPreferenceService _service;
    private readonly ILogger<NotificationPreferencesController> _logger;

    public NotificationPreferencesController(
        INotificationPreferenceService service,
        ILogger<NotificationPreferencesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("my-preferences")]
    [ProducesResponseType(typeof(NotificationPreferencesDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationPreferencesDto>> GetMyPreferences()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var prefs = await _service.GetUserPreferencesAsync(userId);
        return Ok(NotificationPreferencesDto.FromEntity(prefs));
    }

    [HttpPost("my-preferences")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> UpdateMyPreferences([FromBody] NotificationPreferencesDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        await _service.SetUserPreferencesAsync(userId, dto.ToEntity(userId));
        _logger.LogInformation("Notification preferences updated for user {UserId}", userId);
        return Ok();
    }
}
