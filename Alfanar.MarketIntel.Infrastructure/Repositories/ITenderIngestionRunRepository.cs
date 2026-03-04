using Alfanar.MarketIntel.Domain.Entities;

namespace Alfanar.MarketIntel.Application.Interfaces;

public interface ITenderIngestionRunRepository
{
    Task<TenderIngestionRun?> GetByIdAsync(Guid id);
    Task<TenderIngestionRun?> GetLatestBySourceIdAsync(Guid sourceId);
    Task<List<TenderIngestionRun>> GetFailedRunsAsync(int maxItems = 100);
    Task AddAsync(TenderIngestionRun entity);
    Task UpdateAsync(TenderIngestionRun entity);
    Task SaveChangesAsync();
}
