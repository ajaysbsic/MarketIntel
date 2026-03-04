using System.Security.Claims;
using Alfanar.MarketIntel.Application.DTOs;
using Alfanar.MarketIntel.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;

namespace Alfanar.MarketIntel.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationPreferencesController : ControllerBase
{
    private readonly INotificationPreferenceService _service;
    private readonly ILogger<NotificationPreferencesController> _logger;
    private readonly IWebHostEnvironment _environment;

    public NotificationPreferencesController(
        INotificationPreferenceService service,
        ILogger<NotificationPreferencesController> logger,
        IWebHostEnvironment environment)
    {
        _service = service;
        _logger = logger;
        _environment = environment;
    }

    [HttpGet("my-preferences")]
    [ProducesResponseType(typeof(NotificationPreferencesDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationPreferencesDto>> GetMyPreferences()
    {
        var userId = ResolveUserId();
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
        var userId = ResolveUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        await _service.SetUserPreferencesAsync(userId, dto.ToEntity(userId));
        _logger.LogInformation("Notification preferences updated for user {UserId}", userId);
        return Ok();
    }

    private string? ResolveUserId()
    {
        var claimUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrWhiteSpace(claimUserId))
        {
            return claimUserId;
        }

        if (_environment.IsDevelopment())
        {
            return "local-dev-user";
        }

        return null;
    }
}
