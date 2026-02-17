using Alfanar.MarketIntel.Api.Hubs;
using Alfanar.MarketIntel.Application.DTOs;
using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Application.Services;
using Alfanar.MarketIntel.Domain.Entities;
using Alfanar.MarketIntel.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Alfanar.MarketIntel.Api.Controllers;

[ApiController]
[Route("api/alerts")]
public class AlertsController : ControllerBase
{
    private readonly ISmartAlertRepository _alertRepository;
    private readonly IHubContext<NotificationsHub> _hub;
    private readonly ArticleAlertEngine _articleAlertEngine;
    private readonly ISmartAlertNotifier _alertNotifier;
    private readonly ILogger<AlertsController> _logger;

    public AlertsController(
        ISmartAlertRepository alertRepository,
        IHubContext<NotificationsHub> hub,
        ArticleAlertEngine articleAlertEngine,
        ISmartAlertNotifier alertNotifier,
        ILogger<AlertsController> logger)
    {
        _alertRepository = alertRepository;
        _hub = hub;
        _articleAlertEngine = articleAlertEngine;
        _alertNotifier = alertNotifier;
        _logger = logger;
    }

    /// <summary>
    /// Get recent alerts
    /// </summary>
    [HttpGet("recent")]
    public async Task<IActionResult> GetRecent([FromQuery] int count = 10)
    {
        try
        {
            var alerts = await _alertRepository.GetRecentAlertsAsync(count);
            return Ok(alerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent alerts");
            return StatusCode(500, new { message = "Error retrieving alerts" });
        }
    }

    /// <summary>
    /// Get alerts for a specific company
    /// </summary>
    [HttpGet("company/{companyName}")]
    public async Task<IActionResult> GetByCompany(string companyName)
    {
        try
        {
            var alerts = await _alertRepository.GetByCompanyAsync(companyName);
            return Ok(alerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alerts for {Company}", companyName);
            return StatusCode(500, new { message = "Error retrieving alerts" });
        }
    }

    /// <summary>
    /// Get alerts by severity
    /// </summary>
    [HttpGet("severity/{severity}")]
    public async Task<IActionResult> GetBySeverity(string severity)
    {
        try
        {
            var alerts = await _alertRepository.GetBySeverityAsync(severity);
            return Ok(alerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alerts for severity {Severity}", severity);
            return StatusCode(500, new { message = "Error retrieving alerts" });
        }
    }

    /// <summary>
    /// Get alerts by type
    /// </summary>
    [HttpGet("by-type/{alertType}")]
    public async Task<IActionResult> GetByType(string alertType)
    {
        try
        {
            var alerts = await _alertRepository.GetByTypeAsync(alertType);
            return Ok(alerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alerts for type {AlertType}", alertType);
            return StatusCode(500, new { message = "Error retrieving alerts" });
        }
    }

    /// <summary>
    /// Get unacknowledged alerts
    /// </summary>
    [HttpGet("unacknowledged")]
    public async Task<IActionResult> GetUnacknowledged()
    {
        try
        {
            var alerts = await _alertRepository.GetUnacknowledgedAsync();
            return Ok(alerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unacknowledged alerts");
            return StatusCode(500, new { message = "Error retrieving alerts" });
        }
    }

    /// <summary>
    /// Acknowledge an alert
    /// </summary>
    [HttpPost("{id:guid}/acknowledge")]
    public async Task<IActionResult> Acknowledge(Guid id, [FromBody] AcknowledgeRequest request)
    {
        try
        {
            var alert = await _alertRepository.GetByIdAsync(id);
            if (alert == null)
                return NotFound(new { message = "Alert not found" });

            alert.IsAcknowledged = true;
            alert.AcknowledgedAt = DateTime.UtcNow;
            alert.AcknowledgedBy = request.AcknowledgedBy ?? "System";

            await _alertRepository.UpdateAsync(alert);
            await _alertRepository.SaveChangesAsync();

            _logger.LogInformation("Alert {AlertId} acknowledged by {User}", id, alert.AcknowledgedBy);

            return Ok(alert);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging alert {AlertId}", id);
            return StatusCode(500, new { message = "Error acknowledging alert" });
        }
    }

    /// <summary>
    /// Get alert statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            var allAlerts = await _alertRepository.GetRecentAlertsAsync(1000);

            var stats = new
            {
                total = allAlerts.Count,
                critical = allAlerts.Count(a => a.Severity == "Critical"),
                high = allAlerts.Count(a => a.Severity == "High"),
                medium = allAlerts.Count(a => a.Severity == "Medium"),
                unacknowledged = allAlerts.Count(a => !a.IsAcknowledged),
                byType = allAlerts.GroupBy(a => a.AlertType)
                    .Select(g => new { type = g.Key, count = g.Count() })
                    .OrderByDescending(x => x.count)
                    .ToList(),
                recent = allAlerts.Take(10).Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.Severity,
                    a.CompanyName,
                    a.CreatedAt
                })
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alert stats");
            return StatusCode(500, new { message = "Error retrieving stats" });
        }
    }

    /// <summary>
    /// Evaluate an article and generate alerts
    /// </summary>
    [HttpPost("evaluate-article")]
    [ProducesResponseType(typeof(List<SmartAlert>), StatusCodes.Status200OK)]
    public async Task<IActionResult> EvaluateArticle([FromBody] EvaluateArticleRequestDto request)
    {
        try
        {
            var alerts = await _articleAlertEngine.EvaluateArticleAsync(
                request.Title,
                request.Snippet ?? string.Empty,
                request.BodyText,
                request.SourceType,
                request.SourceId,
                request.SourceUrl);

            if (alerts.Count == 0)
                return Ok(alerts);

            await _alertRepository.AddRangeAsync(alerts);
            await _alertRepository.SaveChangesAsync();

            foreach (var alert in alerts.Where(a => a.Severity == "High" || a.Severity == "Critical"))
            {
                await _alertNotifier.NotifyAsync(alert);
            }

            return Ok(alerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating article alerts");
            return StatusCode(500, new { message = "Error evaluating article" });
        }
    }

    /// <summary>
    /// Get summary grouped by alert type for last 7 days
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        try
        {
            var since = DateTime.UtcNow.AddDays(-7);
            var alerts = await _alertRepository.GetRecentAlertsAsync(1000);
            var recent = alerts.Where(a => a.CreatedAt >= since).ToList();

            var summary = recent
                .GroupBy(a => a.AlertType)
                .Select(g => new { type = g.Key, count = g.Count() })
                .OrderByDescending(x => x.count)
                .ToList();

            return Ok(new
            {
                since,
                total = recent.Count,
                byType = summary
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alert summary");
            return StatusCode(500, new { message = "Error retrieving summary" });
        }
    }
}

public class AcknowledgeRequest
{
    public string? AcknowledgedBy { get; set; }
}
