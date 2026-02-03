using Alfanar.MarketIntel.Domain.Entities;
using Alfanar.MarketIntel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alfanar.MarketIntel.Infrastructure.Repositories;

public class NewsRepository : INewsRepository
{
    private readonly MarketIntelDbContext _context;

    public NewsRepository(MarketIntelDbContext context)
    {
        _context = context;
    }

    public async Task<NewsArticle?> GetByIdAsync(Guid id)
    {
        return await _context.NewsArticles
            .Include(n => n.NewsArticleTags)
            .ThenInclude(nat => nat.Tag)
            .Include(n => n.RssFeed)
            .FirstOrDefaultAsync(n => n.Id == id);
    }

    public async Task<NewsArticle?> GetByUrlAsync(string url)
    {
        return await _context.NewsArticles
            .Include(n => n.NewsArticleTags)
            .ThenInclude(nat => nat.Tag)
            .FirstOrDefaultAsync(n => n.Url == url);
    }

    public async Task<List<NewsArticle>> GetAllAsync()
    {
        return await _context.NewsArticles
            .Include(n => n.NewsArticleTags)
            .ThenInclude(nat => nat.Tag)
            .OrderByDescending(n => n.PublishedUtc)
            .ToListAsync();
    }

    public async Task<List<NewsArticle>> GetFilteredAsync(
        string? category = null,
        string? region = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? searchTerm = null,
        List<string>? tags = null,
        int pageNumber = 1,
        int pageSize = 20)
    {
        var query = _context.NewsArticles
            .Include(n => n.NewsArticleTags)
            .ThenInclude(nat => nat.Tag)
            .AsQueryable();

        query = ApplyFilters(query, category, region, fromDate, toDate, searchTerm, tags);

        return await query
            .OrderByDescending(n => n.PublishedUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetFilteredCountAsync(
        string? category = null,
        string? region = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? searchTerm = null,
        List<string>? tags = null)
    {
        var query = _context.NewsArticles.AsQueryable();
        query = ApplyFilters(query, category, region, fromDate, toDate, searchTerm, tags);
        return await query.CountAsync();
    }

    private IQueryable<NewsArticle> ApplyFilters(
        IQueryable<NewsArticle> query,
        string? category,
        string? region,
        DateTime? fromDate,
        DateTime? toDate,
        string? searchTerm,
        List<string>? tags)
    {
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(n => n.Category == category);

        if (!string.IsNullOrWhiteSpace(region))
            query = query.Where(n => n.Region == region);

        if (fromDate.HasValue)
            query = query.Where(n => n.PublishedUtc >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(n => n.PublishedUtc <= toDate.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(n =>
                n.Title.Contains(searchTerm) ||
                n.Summary.Contains(searchTerm) ||
                n.BodyText.Contains(searchTerm));
        }

        if (tags != null && tags.Any())
        {
            var normalizedTags = tags.Select(t => t.ToUpperInvariant()).ToList();
            query = query.Where(n => n.NewsArticleTags
                .Any(nat => normalizedTags.Contains(nat.Tag.NormalizedName)));
        }

        return query;
    }

    public async Task<List<NewsArticle>> GetRecentAsync(int count)
    {
        return await _context.NewsArticles
            .Include(n => n.NewsArticleTags)
            .ThenInclude(nat => nat.Tag)
            .OrderByDescending(n => n.PublishedUtc)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<string>> GetDistinctCategoriesAsync()
    {
        return await _context.NewsArticles
            .Select(n => n.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }

    public async Task<List<string>> GetDistinctRegionsAsync()
    {
        return await _context.NewsArticles
            .Select(n => n.Region)
            .Distinct()
            .OrderBy(r => r)
            .ToListAsync();
    }

    public async Task<bool> ExistsByUrlAsync(string url)
    {
        return await _context.NewsArticles.AnyAsync(n => n.Url == url);
    }

    public async Task AddAsync(NewsArticle article)
    {
        await _context.NewsArticles.AddAsync(article);
    }

    public async Task UpdateAsync(NewsArticle article)
    {
        _context.NewsArticles.Update(article);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(NewsArticle article)
    {
        _context.NewsArticles.Remove(article);
        await Task.CompletedTask;
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
