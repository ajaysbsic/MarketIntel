using Alfanar.MarketIntel.Application.Common;
using Alfanar.MarketIntel.Application.DTOs;

namespace Alfanar.MarketIntel.Application.Interfaces;

public interface IWebSearchService
{
    /// <summary>Performs a real-time web search and caches results in the database</summary>
    Task<Result<List<WebSearchResultDto>>> SearchAsync(WebSearchRequestDto request);

    /// <summary>Retrieves cached search results for a keyword with optional date filtering</summary>
    Task<Result<PagedResultDto<WebSearchResultDto>>> GetCachedResultsAsync(string keyword, DateTime? fromDate = null, DateTime? toDate = null, int pageNumber = 1, int pageSize = 20);

    /// <summary>Gets the total count of cached results for a keyword</summary>
    Task<Result<int>> GetResultCountAsync(string keyword, DateTime? fromDate = null, DateTime? toDate = null);

    /// <summary>Deduplicates search results by URL</summary>
    Task<Result<bool>> DeduplicateResultsAsync(string keyword);
}
