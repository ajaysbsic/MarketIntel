using Alfanar.MarketIntel.Application.Interfaces.ScheduledJobs;
using Alfanar.MarketIntel.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace Alfanar.MarketIntel.Application.Services.ScheduledJobs;

public class AlertProcessingJob : IAlertProcessingJob
{
    private readonly ISmartAlertRepository _alertRepository;
    private readonly ILogger<AlertProcessingJob> _logger;

    public AlertProcessingJob(ISmartAlertRepository alertRepository, ILogger<AlertProcessingJob> logger)
    {
        _alertRepository = alertRepository;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var alerts = await _alertRepository.GetUnacknowledgedAsync();
        if (alerts.Count == 0)
        {
            _logger.LogInformation("Alert processing job found no unacknowledged alerts.");
            return;
        }

        var highCount = alerts.Count(a => a.Severity == "High");
        var criticalCount = alerts.Count(a => a.Severity == "Critical");

        _logger.LogInformation(
            "Alert processing job reviewed {Total} alerts (High: {High}, Critical: {Critical}).",
            alerts.Count,
            highCount,
            criticalCount);
    }
}
