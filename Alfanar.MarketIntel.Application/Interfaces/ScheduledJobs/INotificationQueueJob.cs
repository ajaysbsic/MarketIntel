namespace Alfanar.MarketIntel.Application.Interfaces.ScheduledJobs;

public interface INotificationQueueJob
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
