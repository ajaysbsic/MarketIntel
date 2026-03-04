namespace Alfanar.MarketIntel.Application.Interfaces.ScheduledJobs;

public interface ITenderNotificationDispatchJob
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
