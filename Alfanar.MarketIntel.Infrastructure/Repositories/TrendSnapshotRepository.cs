using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Domain.Entities;
using Alfanar.MarketIntel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alfanar.MarketIntel.Infrastructure.Repositories;

public class TrendSnapshotRepository : ITrendSnapshotRepository
{
    private readonly MarketIntelDbContext _context;

    public TrendSnapshotRepository(MarketIntelDbContext context)
    {
        _context = context;
    }

    public async Task<TrendSnapshot?> GetByKeywordAndDateAsync(string keyword, DateTime date)
    {
        var day = date.Date;
        return await _context.TrendSnapshots
            .FirstOrDefaultAsync(t => t.Keyword == keyword && t.SnapshotDate == day);
    }

    public async Task<List<TrendSnapshot>> GetByKeywordRangeAsync(string keyword, DateTime fromDate, DateTime toDate)
    {
        return await _context.TrendSnapshots
            .Where(t => t.Keyword == keyword && t.SnapshotDate >= fromDate.Date && t.SnapshotDate <= toDate.Date)
            .OrderBy(t => t.SnapshotDate)
            .ToListAsync();
    }

    public async Task AddAsync(TrendSnapshot entity)
    {
        await _context.TrendSnapshots.AddAsync(entity);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
