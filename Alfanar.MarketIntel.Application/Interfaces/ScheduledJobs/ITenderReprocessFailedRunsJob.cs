namespace Alfanar.MarketIntel.Application.Interfaces.ScheduledJobs;

public interface ITenderReprocessFailedRunsJob
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
