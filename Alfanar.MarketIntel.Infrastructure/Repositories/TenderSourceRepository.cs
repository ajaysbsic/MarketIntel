using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Domain.Entities;
using Alfanar.MarketIntel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alfanar.MarketIntel.Infrastructure.Repositories;

public class TenderSourceRepository : ITenderSourceRepository
{
    private readonly MarketIntelDbContext _context;

    public TenderSourceRepository(MarketIntelDbContext context)
    {
        _context = context;
    }

    public async Task<TenderSource?> GetByIdAsync(Guid id)
    {
        return await _context.TenderSources.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<TenderSource?> GetByNameAsync(string name)
    {
        return await _context.TenderSources.FirstOrDefaultAsync(x => x.Name.ToLower() == name.ToLower());
    }

    public async Task<List<TenderSource>> GetAllAsync(bool includeDisabled = true)
    {
        var query = _context.TenderSources.AsQueryable();
        if (!includeDisabled)
        {
            query = query.Where(x => x.IsEnabled);
        }

        return await query
            .OrderBy(x => x.PollPriority)
            .ThenBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<List<TenderSource>> GetEnabledAsync()
    {
        return await _context.TenderSources
            .Where(x => x.IsEnabled)
            .OrderBy(x => x.PollPriority)
            .ThenBy(x => x.Name)
            .ToListAsync();
    }

    public async Task AddAsync(TenderSource entity)
    {
        await _context.TenderSources.AddAsync(entity);
    }

    public async Task UpdateAsync(TenderSource entity)
    {
        _context.TenderSources.Update(entity);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(TenderSource entity)
    {
        _context.TenderSources.Remove(entity);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
