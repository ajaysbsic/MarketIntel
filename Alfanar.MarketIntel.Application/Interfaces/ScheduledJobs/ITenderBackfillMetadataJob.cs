namespace Alfanar.MarketIntel.Application.Interfaces.ScheduledJobs;

public interface ITenderBackfillMetadataJob
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
