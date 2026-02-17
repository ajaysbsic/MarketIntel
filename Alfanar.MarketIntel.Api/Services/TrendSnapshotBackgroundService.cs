using Alfanar.MarketIntel.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Alfanar.MarketIntel.Api.Services;

public class TrendSnapshotBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TrendSnapshotBackgroundService> _logger;

    public TrendSnapshotBackgroundService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<TrendSnapshotBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNextRun();
            _logger.LogInformation("Trend snapshot service sleeping for {Delay} minutes", delay.TotalMinutes);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            if (stoppingToken.IsCancellationRequested)
                break;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var trendService = scope.ServiceProvider.GetRequiredService<ITrendAnalyticsService>();
                var result = await trendService.GenerateDailySnapshotAsync(DateTime.UtcNow.Date);
                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Trend snapshot generation failed: {Error}", result.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating trend snapshot");
            }
        }
    }

    private TimeSpan GetDelayUntilNextRun()
    {
        var snapshotTime = _configuration["Trends:SnapshotTime"] ?? "00:00";
        if (!TimeSpan.TryParse(snapshotTime, out var targetTime))
        {
            targetTime = new TimeSpan(0, 0, 0);
        }

        var now = DateTime.UtcNow;
        var nextRun = now.Date.Add(targetTime);
        if (nextRun <= now)
        {
            nextRun = nextRun.AddDays(1);
        }

        return nextRun - now;
    }
}
