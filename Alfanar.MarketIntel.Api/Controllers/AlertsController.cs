using Alfanar.MarketIntel.Api.Hubs;
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
    private readonly ILogger<AlertsController> _logger;

    public AlertsController(
        ISmartAlertRepository alertRepository,
        IHubContext<NotificationsHub> hub,
        ILogger<AlertsController> logger)
    {
        _alertRepository = alertRepository;
        _hub = hub;
        _logger = logger;
    }

    /// <summary>
    /// Get recent alerts
    /// </summary>
    [HttpGet("recent")]
    public async Task<IActionResult> GetRecent([FromQuery] int count = 50)
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
}

public class AcknowledgeRequest
{
    public string? AcknowledgedBy { get; set; }
}
