using Alfanar.MarketIntel.Application.Common;
using Alfanar.MarketIntel.Application.DTOs;
using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Alfanar.MarketIntel.Application.Services;

/// <summary>
/// Service for web search operations - orchestrates providers and manages caching
/// </summary>
public class WebSearchService : IWebSearchService
{
    private readonly IWebSearchResultRepository _repository;
    private readonly IEnumerable<IWebSearchProvider> _providers;
    private readonly ILogger<WebSearchService> _logger;
    private readonly MarketIntelDbContext _context;

    public WebSearchService(
        IWebSearchResultRepository repository,
        IEnumerable<IWebSearchProvider> providers,
        ILogger<WebSearchService> logger,
        MarketIntelDbContext context)
    {
        _repository = repository;
        _providers = providers;
        _logger = logger;
        _context = context;
    }

    public async Task<Result<List<WebSearchResultDto>>> SearchAsync(WebSearchRequestDto request)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.Keyword))
                return Result<List<WebSearchResultDto>>.Failure("Keyword cannot be empty");

            request.MaxResults = Math.Min(request.MaxResults, 100); // Cap at 100

            // Get the appropriate search provider
            // Priority: Explicit request > NewsAPI (default) > Google (fallback)
            var provider = _providers.FirstOrDefault(p => p.ProviderName.Equals(request.SearchProvider, StringComparison.OrdinalIgnoreCase));
            
            if (provider == null)
            {
                // Default to NewsAPI if available, fallback to Google
                provider = _providers.FirstOrDefault(p => p.ProviderName.Equals("newsapi", StringComparison.OrdinalIgnoreCase))
                    ?? _providers.FirstOrDefault(p => p.ProviderName.Equals("google", StringComparison.OrdinalIgnoreCase));
            }

            if (provider == null)
                return Result<List<WebSearchResultDto>>.Failure("No search provider configured");

            if (!provider.IsConfigured())
                return Result<List<WebSearchResultDto>>.Failure($"Search provider '{provider.ProviderName}' is not properly configured");

            _logger.LogInformation("Searching with {Provider} for: {Keyword}", provider.ProviderName, request.Keyword);

            // Execute search with fallback logic
            List<WebSearchResultDto> results = new();
            try
            {
                results = await provider.SearchAsync(request);
                
                // If primary provider (NewsAPI) returns no results and it was default, try Google as fallback
                if (results.Count == 0 && provider.ProviderName.Equals("newsapi", StringComparison.OrdinalIgnoreCase))
                {
                    var googleProvider = _providers.FirstOrDefault(p => p.ProviderName.Equals("google", StringComparison.OrdinalIgnoreCase));
                    if (googleProvider != null && googleProvider.IsConfigured())
                    {
                        _logger.LogInformation("NewsAPI returned no results, attempting fallback to Google for: {Keyword}", request.Keyword);
                        results = await googleProvider.SearchAsync(request);
                        provider = googleProvider;
                    }
                }
            }
            catch (Exception ex)
            {
                // If primary provider fails and it's NewsAPI, try Google
                if (provider.ProviderName.Equals("newsapi", StringComparison.OrdinalIgnoreCase))
                {
                    var googleProvider = _providers.FirstOrDefault(p => p.ProviderName.Equals("google", StringComparison.OrdinalIgnoreCase));
                    if (googleProvider != null && googleProvider.IsConfigured())
                    {
                        _logger.LogWarning(ex, "NewsAPI failed, attempting fallback to Google for: {Keyword}", request.Keyword);
                        try
                        {
                            results = await googleProvider.SearchAsync(request);
                            provider = googleProvider;
                        }
                        catch (Exception googleEx)
                        {
                            _logger.LogError(googleEx, "Both NewsAPI and Google search failed for: {Keyword}", request.Keyword);
                            return Result<List<WebSearchResultDto>>.Failure($"Search error: {googleEx.Message}");
                        }
                    }
                    else
                    {
                        throw;
                    }
                }
                else
                {
                    throw;
                }
            }

            // Cache results in database (deduplicate by URL+keyword)
            foreach (var result in results)
            {
                var existing = await _repository.GetByUrlAndKeywordAsync(result.Url, result.Keyword);
                if (existing == null)
                {
                    var entity = new Domain.Entities.WebSearchResult
                    {
                        Keyword = result.Keyword,
                        Title = result.Title,
                        Snippet = result.Snippet,
                        Url = result.Url,
                        PublishedDate = result.PublishedDate,
                        Source = result.Source,
                        SearchProvider = provider.ProviderName,
                        RetrievedUtc = result.RetrievedUtc,
                        IsFromMonitoring = result.IsFromMonitoring
                    };

                    await _repository.AddAsync(entity);
                }
            }

            await _repository.SaveChangesAsync();

            _logger.LogInformation("Cached {Count} search results for: {Keyword}", results.Count, request.Keyword);

            return Result<List<WebSearchResultDto>>.Success(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing web search for: {Keyword}", request.Keyword);
            return Result<List<WebSearchResultDto>>.Failure($"Search error: {ex.Message}");
        }
    }

    public async Task<Result<PagedResultDto<WebSearchResultDto>>> GetCachedResultsAsync(string keyword, DateTime? fromDate = null, DateTime? toDate = null, int pageNumber = 1, int pageSize = 20)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return Result<PagedResultDto<WebSearchResultDto>>.Failure("Keyword cannot be empty");

            pageSize = Math.Min(pageSize, 100); // Cap at 100

            // Get results
            var results = await _repository.GetCachedResultsAsync(keyword, fromDate, toDate, pageNumber, pageSize);

            // Get total count
            int totalCount;
            if (fromDate.HasValue && toDate.HasValue)
            {
                totalCount = await _repository.GetResultCountByKeywordAndDateRangeAsync(keyword, fromDate.Value, toDate.Value);
            }
            else
            {
                totalCount = await _repository.GetResultCountByKeywordAsync(keyword);
            }

            var dto = new PagedResultDto<WebSearchResultDto>
            {
                Items = results.Select(r => MapToDto(r)).ToList(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Result<PagedResultDto<WebSearchResultDto>>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cached results for: {Keyword}", keyword);
            return Result<PagedResultDto<WebSearchResultDto>>.Failure($"Error retrieving results: {ex.Message}");
        }
    }

    public async Task<Result<int>> GetResultCountAsync(string keyword, DateTime? fromDate = null, DateTime? toDate = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return Result<int>.Failure("Keyword cannot be empty");

            int count;
            if (fromDate.HasValue && toDate.HasValue)
            {
                count = await _repository.GetResultCountByKeywordAndDateRangeAsync(keyword, fromDate.Value, toDate.Value);
            }
            else
            {
                count = await _repository.GetResultCountByKeywordAsync(keyword);
            }

            return Result<int>.Success(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting result count for: {Keyword}", keyword);
            return Result<int>.Failure($"Error retrieving count: {ex.Message}");
        }
    }

    public async Task<Result<bool>> DeduplicateResultsAsync(string keyword)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return Result<bool>.Failure("Keyword cannot be empty");

            // Get all results for keyword, grouped by URL
            var results = await _repository.GetResultsByKeywordAsync(keyword, 1, 10000);

            var urlGroups = results.GroupBy(r => r.Url).Where(g => g.Count() > 1).ToList();

            if (urlGroups.Any())
            {
                // Keep the oldest entry for each URL, delete duplicates
                foreach (var group in urlGroups)
                {
                    var toKeep = group.OrderBy(r => r.RetrievedUtc).First();
                    var toDelete = group.Where(r => r.Id != toKeep.Id);

                    foreach (var item in toDelete)
                    {
                        await _repository.DeleteAsync(item);
                    }
                }

                await _repository.SaveChangesAsync();
                _logger.LogInformation("Deduplicated {Count} results for keyword: {Keyword}", urlGroups.Count, keyword);
            }

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deduplicating results for: {Keyword}", keyword);
            return Result<bool>.Failure($"Error deduplicating: {ex.Message}");
        }
    }

    private WebSearchResultDto MapToDto(Domain.Entities.WebSearchResult entity)
    {
        return new WebSearchResultDto
        {
            Id = entity.Id,
            Keyword = entity.Keyword,
            Title = entity.Title,
            Snippet = entity.Snippet,
            Url = entity.Url,
            PublishedDate = entity.PublishedDate,
            Source = entity.Source,
            RetrievedUtc = entity.RetrievedUtc,
            IsFromMonitoring = entity.IsFromMonitoring
        };
    }
}
