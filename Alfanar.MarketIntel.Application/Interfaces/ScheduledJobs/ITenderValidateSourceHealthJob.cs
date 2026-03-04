namespace Alfanar.MarketIntel.Application.Interfaces.ScheduledJobs;

public interface ITenderValidateSourceHealthJob
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
