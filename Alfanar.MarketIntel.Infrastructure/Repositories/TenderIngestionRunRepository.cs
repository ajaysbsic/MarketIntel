using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Domain.Entities;
using Alfanar.MarketIntel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alfanar.MarketIntel.Infrastructure.Repositories;

public class TenderIngestionRunRepository : ITenderIngestionRunRepository
{
    private readonly MarketIntelDbContext _context;

    public TenderIngestionRunRepository(MarketIntelDbContext context)
    {
        _context = context;
    }

    public async Task<TenderIngestionRun?> GetByIdAsync(Guid id)
    {
        return await _context.TenderIngestionRuns
            .Include(x => x.Source)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<TenderIngestionRun?> GetLatestBySourceIdAsync(Guid sourceId)
    {
        return await _context.TenderIngestionRuns
            .Where(x => x.SourceId == sourceId)
            .OrderByDescending(x => x.StartedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<TenderIngestionRun>> GetFailedRunsAsync(int maxItems = 100)
    {
        return await _context.TenderIngestionRuns
            .Include(x => x.Source)
            .Where(x => x.Status == "Failed")
            .OrderByDescending(x => x.StartedAt)
            .Take(maxItems)
            .ToListAsync();
    }

    public async Task AddAsync(TenderIngestionRun entity)
    {
        await _context.TenderIngestionRuns.AddAsync(entity);
    }

    public async Task UpdateAsync(TenderIngestionRun entity)
    {
        _context.TenderIngestionRuns.Update(entity);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
