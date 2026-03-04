using Alfanar.MarketIntel.Application.Interfaces.ScheduledJobs;
using Alfanar.MarketIntel.Domain.Entities;
using Alfanar.MarketIntel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Alfanar.MarketIntel.Application.Services.ScheduledJobs;

public class TenderValidateSourceHealthJob : ITenderValidateSourceHealthJob
{
    private readonly MarketIntelDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TenderValidateSourceHealthJob> _logger;

    public TenderValidateSourceHealthJob(
        MarketIntelDbContext dbContext,
        IConfiguration configuration,
        ILogger<TenderValidateSourceHealthJob> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var staleHours = _configuration.GetValue("TenderMonitoring:HealthStaleHours", 6);
        var cutoff = DateTime.UtcNow.AddHours(-staleHours);

        var sources = await _dbContext.TenderSources
            .Where(x => x.IsEnabled)
            .ToListAsync(cancellationToken);

        foreach (var source in sources)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var latestRun = await _dbContext.TenderIngestionRuns
                .Where(x => x.SourceId == source.Id)
                .OrderByDescending(x => x.StartedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (latestRun != null && latestRun.StartedAt >= cutoff)
            {
                continue;
            }

            var healthFailure = new TenderIngestionRun
            {
                SourceId = source.Id,
                StartedAt = DateTime.UtcNow,
                EndedAt = DateTime.UtcNow,
                Status = "Failed",
                ItemsFetched = 0,
                ItemsNew = 0,
                ItemsUpdated = 0,
                Errors = $"Health check failed: no successful ingestion within last {staleHours} hours.",
                RetryCount = 0,
                WorkerId = "hangfire-health"
            };

            await _dbContext.TenderIngestionRuns.AddAsync(healthFailure, cancellationToken);
            _logger.LogWarning("Tender source health stale for {SourceName}", source.Name);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
