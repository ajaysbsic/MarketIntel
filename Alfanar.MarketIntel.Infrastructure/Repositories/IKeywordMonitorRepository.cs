using Alfanar.MarketIntel.Domain.Entities;

namespace Alfanar.MarketIntel.Application.Interfaces;

public interface IKeywordMonitorRepository
{
    Task<KeywordMonitor?> GetByIdAsync(Guid id);
    Task<List<KeywordMonitor>> GetAllAsync();
    Task AddAsync(KeywordMonitor entity);
    Task UpdateAsync(KeywordMonitor entity);
    Task DeleteAsync(KeywordMonitor entity);
    Task SaveChangesAsync();
    Task<KeywordMonitor?> GetByKeywordAsync(string keyword);
    Task<List<KeywordMonitor>> GetActiveMonitorsAsync();
    Task<List<KeywordMonitor>> GetMonitorsDueForCheckAsync(int intervalMinutes);
    Task<KeywordMonitor?> GetByIdWithResultsAsync(Guid id);
}
