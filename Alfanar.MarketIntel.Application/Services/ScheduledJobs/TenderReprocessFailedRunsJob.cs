using Alfanar.MarketIntel.Application.Interfaces.ScheduledJobs;
using Alfanar.MarketIntel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Alfanar.MarketIntel.Application.Services.ScheduledJobs;

public class TenderReprocessFailedRunsJob : ITenderReprocessFailedRunsJob
{
    private readonly MarketIntelDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TenderReprocessFailedRunsJob> _logger;

    public TenderReprocessFailedRunsJob(
        MarketIntelDbContext dbContext,
        IConfiguration configuration,
        ILogger<TenderReprocessFailedRunsJob> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var maxRetries = _configuration.GetValue("TenderMonitoring:MaxReprocessRetries", 3);
        var batchSize = _configuration.GetValue("TenderMonitoring:ReprocessBatchSize", 50);

        var failedRuns = await _dbContext.TenderIngestionRuns
            .Where(x => x.Status == "Failed" && x.RetryCount < maxRetries)
            .OrderBy(x => x.StartedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        foreach (var run in failedRuns)
        {
            cancellationToken.ThrowIfCancellationRequested();

            run.RetryCount += 1;
            run.Status = run.RetryCount >= maxRetries ? "Failed_MaxRetries" : "QueuedForRetry";
            run.EndedAt = DateTime.UtcNow;

            _logger.LogInformation("Tender failed run {RunId} marked as {Status} (retry {RetryCount})", run.Id, run.Status, run.RetryCount);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
