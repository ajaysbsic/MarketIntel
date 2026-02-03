using Alfanar.MarketIntel.Domain.Entities;

namespace Alfanar.MarketIntel.Infrastructure.Repositories;

public interface INewsRepository
{
    Task<NewsArticle?> GetByIdAsync(Guid id);
    Task<NewsArticle?> GetByUrlAsync(string url);
    Task<List<NewsArticle>> GetAllAsync();
    Task<List<NewsArticle>> GetFilteredAsync(
        string? category = null, 
        string? region = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? searchTerm = null,
        List<string>? tags = null,
        int pageNumber = 1,
        int pageSize = 20);
    Task<int> GetFilteredCountAsync(
        string? category = null, 
        string? region = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? searchTerm = null,
        List<string>? tags = null);
    Task<List<NewsArticle>> GetRecentAsync(int count);
    Task<List<string>> GetDistinctCategoriesAsync();
    Task<List<string>> GetDistinctRegionsAsync();
    Task<bool> ExistsByUrlAsync(string url);
    Task AddAsync(NewsArticle article);
    Task UpdateAsync(NewsArticle article);
    Task DeleteAsync(NewsArticle article);
    Task<int> SaveChangesAsync();
}
