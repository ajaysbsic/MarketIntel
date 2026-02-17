using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Application.Interfaces.ScheduledJobs;
using Microsoft.Extensions.Logging;

namespace Alfanar.MarketIntel.Application.Services.ScheduledJobs;

public class TrendSnapshotJob : ITrendSnapshotJob
{
    private readonly ITrendAnalyticsService _trendAnalyticsService;
    private readonly ILogger<TrendSnapshotJob> _logger;

    public TrendSnapshotJob(ITrendAnalyticsService trendAnalyticsService, ILogger<TrendSnapshotJob> logger)
    {
        _trendAnalyticsService = trendAnalyticsService;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var result = await _trendAnalyticsService.GenerateDailySnapshotAsync(DateTime.UtcNow.Date);
        if (!result.IsSuccess)
        {
            _logger.LogWarning("Trend snapshot job failed: {Error}", result.Error);
        }
    }
}
