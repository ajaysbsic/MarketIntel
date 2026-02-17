namespace Alfanar.MarketIntel.Application.Interfaces.ScheduledJobs;

public interface IKeywordMonitorJob
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
