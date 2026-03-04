namespace Alfanar.MarketIntel.Application.Interfaces.ScheduledJobs;

public interface ITenderDailyIntegrityCheckJob
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
