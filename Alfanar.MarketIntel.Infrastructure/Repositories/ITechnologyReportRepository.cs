using Alfanar.MarketIntel.Domain.Entities;

namespace Alfanar.MarketIntel.Application.Interfaces;

public interface ITechnologyReportRepository
{
    Task<TechnologyReport?> GetByIdAsync(Guid id);
    Task<List<TechnologyReport>> GetAllAsync();
    Task AddAsync(TechnologyReport entity);
    Task UpdateAsync(TechnologyReport entity);
    Task DeleteAsync(TechnologyReport entity);
    Task SaveChangesAsync();
    Task<TechnologyReport?> GetByIdWithResultsAsync(Guid id);
    Task<List<TechnologyReport>> GetReportsAsync(int pageNumber = 1, int pageSize = 10);
    Task<List<TechnologyReport>> GetReportsForKeywordAsync(string keyword, int pageNumber = 1, int pageSize = 10);
    Task<List<TechnologyReport>> GetReportsForDateRangeAsync(DateTime startDate, DateTime endDate, int pageNumber = 1, int pageSize = 10);
    Task<int> GetReportsCountAsync();
}
