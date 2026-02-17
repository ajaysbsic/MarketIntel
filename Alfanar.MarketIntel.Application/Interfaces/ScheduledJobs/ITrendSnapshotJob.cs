namespace Alfanar.MarketIntel.Application.Interfaces.ScheduledJobs;

public interface ITrendSnapshotJob
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
