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
        return await _context.WebSearchResults
            .FirstOrDefaultAsync(w => w.Url == url && w.Keyword == keyword);
    }

    public async Task<List<WebSearchResult>> GetResultsByKeywordAsync(string keyword, int pageNumber = 1, int pageSize = 20)
    {
        return await _context.WebSearchResults
            .Where(w => w.Keyword == keyword)
            .OrderByDescending(w => w.RetrievedUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<WebSearchResult>> GetResultsByKeywordAndDateRangeAsync(string keyword, DateTime fromDate, DateTime toDate)
    {
        return await _context.WebSearchResults
            .Where(w => w.Keyword == keyword 
                && w.PublishedDate >= fromDate 
                && w.PublishedDate <= toDate)
            .OrderByDescending(w => w.PublishedDate)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<int> GetResultCountByKeywordAsync(string keyword)
    {
        return await _context.WebSearchResults
            .Where(w => w.Keyword == keyword)
            .CountAsync();
    }

    public async Task<int> GetResultCountByKeywordAndDateRangeAsync(string keyword, DateTime fromDate, DateTime toDate)
    {
        return await _context.WebSearchResults
            .Where(w => w.Keyword == keyword 
                && w.PublishedDate >= fromDate 
                && w.PublishedDate <= toDate)
            .CountAsync();
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
        var query = _context.WebSearchResults
            .Where(w => w.Keyword == keyword);

        if (fromDate.HasValue)
            query = query.Where(w => w.PublishedDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(w => w.PublishedDate <= toDate.Value);

        return await query
            .OrderByDescending(w => w.PublishedDate ?? w.RetrievedUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();
    }
}
