using Alfanar.MarketIntel.Domain.Entities;

namespace Alfanar.MarketIntel.Application.Interfaces;

public interface IWebSearchResultRepository
{
    Task<WebSearchResult?> GetByIdAsync(Guid id);
    Task<List<WebSearchResult>> GetAllAsync();
    Task AddAsync(WebSearchResult entity);
    Task UpdateAsync(WebSearchResult entity);
    Task DeleteAsync(WebSearchResult entity);
    Task SaveChangesAsync();
    Task<WebSearchResult?> GetByUrlAndKeywordAsync(string url, string keyword);
    Task<List<WebSearchResult>> GetResultsByKeywordAsync(string keyword, int pageNumber = 1, int pageSize = 20);
    Task<List<WebSearchResult>> GetResultsByKeywordAndDateRangeAsync(string keyword, DateTime fromDate, DateTime toDate);
    Task<int> GetResultCountByKeywordAsync(string keyword);
    Task<int> GetResultCountByKeywordAndDateRangeAsync(string keyword, DateTime fromDate, DateTime toDate);
    Task<List<WebSearchResult>> GetResultsByMonitorIdAsync(Guid monitorId);
    Task<List<WebSearchResult>> GetCachedResultsAsync(string keyword, DateTime? fromDate, DateTime? toDate, int pageNumber = 1, int pageSize = 20);
}
