namespace Alfanar.MarketIntel.Application.Interfaces.ScheduledJobs;

public interface IRssFeedPollerJob
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
