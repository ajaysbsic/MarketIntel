using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Domain.Entities;
using Alfanar.MarketIntel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alfanar.MarketIntel.Infrastructure.Repositories;

public class KeywordMonitorRepository : IKeywordMonitorRepository
{
    private readonly MarketIntelDbContext _context;

    public KeywordMonitorRepository(MarketIntelDbContext context)
    {
        _context = context;
    }

    public async Task<KeywordMonitor?> GetByIdAsync(Guid id)
    {
        return await _context.KeywordMonitors.FindAsync(id);
    }

    public async Task<List<KeywordMonitor>> GetAllAsync()
    {
        return await _context.KeywordMonitors.ToListAsync();
    }

    public async Task AddAsync(KeywordMonitor entity)
    {
        await _context.KeywordMonitors.AddAsync(entity);
    }

    public async Task UpdateAsync(KeywordMonitor entity)
    {
        _context.KeywordMonitors.Update(entity);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(KeywordMonitor entity)
    {
        _context.KeywordMonitors.Remove(entity);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<KeywordMonitor?> GetByKeywordAsync(string keyword)
    {
        return await _context.KeywordMonitors
            .FirstOrDefaultAsync(k => k.Keyword.ToLower() == keyword.ToLower());
    }

    public async Task<List<KeywordMonitor>> GetActiveMonitorsAsync()
    {
        return await _context.KeywordMonitors
            .Where(k => k.IsActive)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<KeywordMonitor>> GetMonitorsDueForCheckAsync(int intervalMinutes)
    {
        var now = DateTime.UtcNow;
        var dueTime = now.AddMinutes(-intervalMinutes);

        return await _context.KeywordMonitors
            .Where(k => k.IsActive && (k.LastCheckedUtc == null || k.LastCheckedUtc <= dueTime))
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<KeywordMonitor?> GetByIdWithResultsAsync(Guid id)
    {
        return await _context.KeywordMonitors
            .Include(k => k.WebSearchResults)
            .FirstOrDefaultAsync(k => k.Id == id);
    }
}
