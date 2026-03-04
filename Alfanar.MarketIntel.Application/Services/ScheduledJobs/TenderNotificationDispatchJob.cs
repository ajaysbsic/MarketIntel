using Alfanar.MarketIntel.Application.Interfaces.ScheduledJobs;
using Alfanar.MarketIntel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Alfanar.MarketIntel.Application.Services.ScheduledJobs;

public class TenderNotificationDispatchJob : ITenderNotificationDispatchJob
{
    private readonly MarketIntelDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TenderNotificationDispatchJob> _logger;

    public TenderNotificationDispatchJob(
        MarketIntelDbContext dbContext,
        IConfiguration configuration,
        ILogger<TenderNotificationDispatchJob> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var batchSize = _configuration.GetValue("TenderMonitoring:NotificationDispatchBatchSize", 200);

        var queued = await _dbContext.TenderNotificationLogs
            .Where(x => x.DeliveryStatus == "Queued")
            .OrderBy(x => x.SentAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        foreach (var item in queued)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.Equals(item.Channel, "InApp", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(item.Channel, "Email", StringComparison.OrdinalIgnoreCase))
            {
                item.DeliveryStatus = "Sent";
                item.SentAt = DateTime.UtcNow;
            }
            else
            {
                item.DeliveryStatus = "Skipped_UnsupportedChannel";
            }
        }

        if (queued.Count > 0)
        {
            _logger.LogInformation("Tender notification dispatch processed {Count} queued notification(s)", queued.Count);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
