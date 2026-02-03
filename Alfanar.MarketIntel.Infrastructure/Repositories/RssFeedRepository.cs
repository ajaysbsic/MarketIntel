using Alfanar.MarketIntel.Domain.Entities;
using Alfanar.MarketIntel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alfanar.MarketIntel.Infrastructure.Repositories;

public class RssFeedRepository : IRssFeedRepository
{
    private readonly MarketIntelDbContext _context;

    public RssFeedRepository(MarketIntelDbContext context)
    {
        _context = context;
    }

    public async Task<RssFeed?> GetByIdAsync(Guid id)
    {
        return await _context.RssFeeds
            .Include(f => f.Articles)
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<RssFeed?> GetByUrlAsync(string url)
    {
        return await _context.RssFeeds
            .FirstOrDefaultAsync(f => f.Url == url);
    }

    public async Task<List<RssFeed>> GetAllAsync()
    {
        return await _context.RssFeeds
            .OrderBy(f => f.Name)
            .ToListAsync();
    }

    public async Task<List<RssFeed>> GetActiveAsync()
    {
        return await _context.RssFeeds
            .Where(f => f.IsActive)
            .OrderBy(f => f.Name)
            .ToListAsync();
    }

    public async Task<bool> ExistsByUrlAsync(string url)
    {
        return await _context.RssFeeds.AnyAsync(f => f.Url == url);
    }

    public async Task AddAsync(RssFeed feed)
    {
        await _context.RssFeeds.AddAsync(feed);
    }

    public async Task UpdateAsync(RssFeed feed)
    {
        _context.RssFeeds.Update(feed);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(RssFeed feed)
    {
        _context.RssFeeds.Remove(feed);
        await Task.CompletedTask;
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
