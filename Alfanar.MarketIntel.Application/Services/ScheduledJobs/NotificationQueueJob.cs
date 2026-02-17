using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Application.Interfaces.ScheduledJobs;
using Microsoft.Extensions.Logging;

namespace Alfanar.MarketIntel.Application.Services.ScheduledJobs;

public class NotificationQueueJob : INotificationQueueJob
{
    private readonly INotificationQueueService _queueService;
    private readonly ILogger<NotificationQueueJob> _logger;

    public NotificationQueueJob(INotificationQueueService queueService, ILogger<NotificationQueueJob> logger)
    {
        _queueService = queueService;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing notification queue");
        await _queueService.ProcessNotificationQueueAsync(cancellationToken);
    }
}
