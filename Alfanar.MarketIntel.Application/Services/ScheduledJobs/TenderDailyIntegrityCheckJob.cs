using Alfanar.MarketIntel.Application.Interfaces.ScheduledJobs;
using Alfanar.MarketIntel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Alfanar.MarketIntel.Application.Services.ScheduledJobs;

public class TenderDailyIntegrityCheckJob : ITenderDailyIntegrityCheckJob
{
    private readonly MarketIntelDbContext _dbContext;
    private readonly ILogger<TenderDailyIntegrityCheckJob> _logger;

    public TenderDailyIntegrityCheckJob(
        MarketIntelDbContext dbContext,
        ILogger<TenderDailyIntegrityCheckJob> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var noticesNeedingFix = await _dbContext.TenderNotices
            .Where(x => x.CurrentVersionId == null)
            .ToListAsync(cancellationToken);

        var fixedCount = 0;
        foreach (var notice in noticesNeedingFix)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var latestVersion = await _dbContext.TenderVersions
                .Where(v => v.TenderNoticeId == notice.Id)
                .OrderByDescending(v => v.VersionNo)
                .FirstOrDefaultAsync(cancellationToken);

            if (latestVersion == null)
            {
                continue;
            }

            notice.CurrentVersionId = latestVersion.Id;
            fixedCount++;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Tender integrity check completed. Fixed {FixedCount} notice record(s).", fixedCount);
    }
}
