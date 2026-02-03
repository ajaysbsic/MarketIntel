using Alfanar.MarketIntel.Application.Common;
using Alfanar.MarketIntel.Application.DTOs;

namespace Alfanar.MarketIntel.Application.Interfaces;

public interface INewsService
{
    Task<Result<NewsArticleDto>> IngestArticleAsync(IngestNewsRequest request);
    Task<Result<NewsArticleDto>> GetByIdAsync(Guid id);
    Task<Result<PaginatedList<NewsArticleDto>>> GetFilteredArticlesAsync(NewsFilterDto filter);
    Task<Result<List<NewsArticleDto>>> GetRecentArticlesAsync(int count = 10);
    Task<Result> DeleteArticleAsync(Guid id);
    Task<Result<List<string>>> GetAllCategoriesAsync();
    Task<Result<List<string>>> GetAllRegionsAsync();
}
