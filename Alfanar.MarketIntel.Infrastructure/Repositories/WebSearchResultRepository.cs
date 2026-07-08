using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Domain.Entities;
using Alfanar.MarketIntel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alfanar.MarketIntel.Infrastructure.Repositories;

public class WebSearchResultRepository : IWebSearchResultRepository
{
    private readonly MarketIntelDbContext _context;

    public WebSearchResultRepository(MarketIntelDbContext context)
    {
        _context = context;
    }

    public async Task<WebSearchResult?> GetByIdAsync(Guid id)
    {
        return await _context.WebSearchResults.FindAsync(id);
    }

    public async Task<List<WebSearchResult>> GetAllAsync()
    {
        return await _context.WebSearchResults.ToListAsync();
    }

    public async Task AddAsync(WebSearchResult entity)
    {
        await _context.WebSearchResults.AddAsync(entity);
    }

    public async Task UpdateAsync(WebSearchResult entity)
    {
        _context.WebSearchResults.Update(entity);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(WebSearchResult entity)
    {
        _context.WebSearchResults.Remove(entity);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<WebSearchResult?> GetByUrlAndKeywordAsync(string url, string keyword)
    {
        var normalizedKeyword = NormalizeKeyword(keyword);

        var matches = await _context.WebSearchResults
            .Where(w => w.Url == url)
            .AsNoTracking()
            .ToListAsync();

        return matches.FirstOrDefault(w => NormalizeKeyword(w.Keyword) == normalizedKeyword);
    }

    public async Task<List<WebSearchResult>> GetResultsByKeywordAsync(string keyword, int pageNumber = 1, int pageSize = 20)
    {
        var results = await _context.WebSearchResults
            .OrderByDescending(w => w.RetrievedUtc)
            .AsNoTracking()
            .ToListAsync();

        return FilterByKeyword(results, keyword)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    public async Task<List<WebSearchResult>> GetResultsByKeywordAndDateRangeAsync(string keyword, DateTime fromDate, DateTime toDate)
    {
        var results = await _context.WebSearchResults
            .Where(w => (w.PublishedDate != null && w.PublishedDate >= fromDate && w.PublishedDate <= toDate)
                || (w.PublishedDate == null && w.RetrievedUtc >= fromDate && w.RetrievedUtc <= toDate))
            .OrderByDescending(w => w.PublishedDate ?? w.RetrievedUtc)
            .AsNoTracking()
            .ToListAsync();

        return FilterByKeyword(results, keyword).ToList();
    }

    public async Task<int> GetResultCountByKeywordAsync(string keyword)
    {
        var results = await _context.WebSearchResults
            .AsNoTracking()
            .ToListAsync();

        return FilterByKeyword(results, keyword).Count();
    }

    public async Task<int> GetResultCountByKeywordAndDateRangeAsync(string keyword, DateTime fromDate, DateTime toDate)
    {
        var results = await _context.WebSearchResults
            .Where(w => (w.PublishedDate != null && w.PublishedDate >= fromDate && w.PublishedDate <= toDate)
                || (w.PublishedDate == null && w.RetrievedUtc >= fromDate && w.RetrievedUtc <= toDate))
            .AsNoTracking()
            .ToListAsync();

        return FilterByKeyword(results, keyword).Count();
    }

    public async Task<List<WebSearchResult>> GetResultsByMonitorIdAsync(Guid monitorId)
    {
        return await _context.WebSearchResults
            .Where(w => w.KeywordMonitorId == monitorId)
            .OrderByDescending(w => w.RetrievedUtc)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<WebSearchResult>> GetCachedResultsAsync(string keyword, DateTime? fromDate, DateTime? toDate, int pageNumber = 1, int pageSize = 20)
    {
        var query = _context.WebSearchResults.AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(w => w.PublishedDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(w => w.PublishedDate <= toDate.Value);

        var results = await query
            .OrderByDescending(w => w.PublishedDate ?? w.RetrievedUtc)
            .AsNoTracking()
            .ToListAsync();

        return FilterByKeyword(results, keyword)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    private static IEnumerable<WebSearchResult> FilterByKeyword(IEnumerable<WebSearchResult> results, string keyword)
    {
        var normalizedKeyword = NormalizeKeyword(keyword);
        if (string.IsNullOrWhiteSpace(normalizedKeyword))
            return Enumerable.Empty<WebSearchResult>();

        var exactMatches = results
            .Where(result => NormalizeKeyword(result.Keyword) == normalizedKeyword)
            .ToList();

        if (exactMatches.Count > 0)
            return exactMatches;

        var tokens = Tokenize(keyword);
        if (tokens.Count == 0)
            return exactMatches;

        return results.Where(result =>
        {
            var combined = NormalizeKeyword($"{result.Keyword} {result.Title} {result.Snippet} {result.Source}");
            return combined.Contains(normalizedKeyword, StringComparison.Ordinal)
                || tokens.All(token => combined.Contains(token, StringComparison.Ordinal));
        });
    }

    private static List<string> Tokenize(string keyword)
    {
        return NormalizeKeyword(keyword)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(token => token.Length > 1)
            .Distinct()
            .ToList();
    }

    private static string NormalizeKeyword(string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return string.Empty;

        return string.Join(' ', keyword
            .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            .ToLowerInvariant();
    }
}
