using Alfanar.MarketIntel.Domain.Entities;

namespace Alfanar.MarketIntel.Infrastructure.Repositories;

public interface IIntelligenceReportRepository
{
    Task<IntelligenceReport?> GetByIdAsync(Guid id);

    Task<List<IntelligenceReport>> GetAllAsync();

    Task AddAsync(IntelligenceReport entity);

    Task UpdateAsync(IntelligenceReport entity);

    Task DeleteAsync(IntelligenceReport entity);

    Task SaveChangesAsync();

    Task<IntelligenceReport?> GetByIdWithResultsAsync(Guid id);

    Task<List<IntelligenceReport>> GetReportsAsync(int pageNumber = 1, int pageSize = 10);

    Task<List<IntelligenceReport>> GetReportsByKeywordAsync(string keyword, int pageNumber = 1, int pageSize = 10);

    Task<List<IntelligenceReport>> GetReportsByStatusAsync(string status, int pageNumber = 1, int pageSize = 10);

    Task<IntelligenceReport?> GetMostRecentForKeywordAsync(string keyword);

    Task<int> GetReportsCountAsync();

    Task<int> GetReportsCountByKeywordAsync(string keyword);
}
