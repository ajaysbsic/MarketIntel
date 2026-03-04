using Alfanar.MarketIntel.Application.Interfaces.ScheduledJobs;
using Alfanar.MarketIntel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Alfanar.MarketIntel.Application.Services.ScheduledJobs;

public class TenderBackfillMetadataJob : ITenderBackfillMetadataJob
{
    private readonly MarketIntelDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TenderBackfillMetadataJob> _logger;

    public TenderBackfillMetadataJob(
        MarketIntelDbContext dbContext,
        IConfiguration configuration,
        ILogger<TenderBackfillMetadataJob> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var batchSize = _configuration.GetValue("TenderMonitoring:BackfillBatchSize", 300);
        var notices = await _dbContext.TenderNotices
            .Where(x => string.IsNullOrWhiteSpace(x.Sector) || string.IsNullOrWhiteSpace(x.Category))
            .OrderBy(x => x.LastChangedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        foreach (var notice in notices)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(notice.Sector))
            {
                notice.Sector = "General";
            }

            if (string.IsNullOrWhiteSpace(notice.Category))
            {
                notice.Category = "Uncategorized";
            }
        }

        if (notices.Count > 0)
        {
            _logger.LogInformation("Tender metadata backfill updated {Count} notice(s)", notices.Count);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
