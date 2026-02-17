using Alfanar.MarketIntel.Domain.Entities;

namespace Alfanar.MarketIntel.Application.Interfaces;

public interface ITrendSnapshotRepository
{
    Task<TrendSnapshot?> GetByKeywordAndDateAsync(string keyword, DateTime date);
    Task<List<TrendSnapshot>> GetByKeywordRangeAsync(string keyword, DateTime fromDate, DateTime toDate);
    Task AddAsync(TrendSnapshot entity);
    Task SaveChangesAsync();
}
