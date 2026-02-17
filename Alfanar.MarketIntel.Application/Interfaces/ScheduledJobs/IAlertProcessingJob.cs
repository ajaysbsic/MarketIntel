namespace Alfanar.MarketIntel.Application.Interfaces.ScheduledJobs;

public interface IAlertProcessingJob
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
