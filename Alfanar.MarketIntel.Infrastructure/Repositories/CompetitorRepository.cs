using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Domain.Entities;
using Alfanar.MarketIntel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alfanar.MarketIntel.Infrastructure.Repositories;

public class CompetitorRepository : ICompetitorRepository
{
    private readonly MarketIntelDbContext _context;

    public CompetitorRepository(MarketIntelDbContext context)
    {
        _context = context;
    }

    public async Task<Competitor?> GetByIdAsync(Guid id)
    {
        return await _context.Competitors.FindAsync(id);
    }

    public async Task<List<Competitor>> GetAllAsync(bool includeInactive = true)
    {
        var query = _context.Competitors.AsQueryable();
        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        return await query.OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<Competitor?> GetByNameAsync(string name)
    {
        return await _context.Competitors.FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());
    }

    public async Task AddAsync(Competitor entity)
    {
        await _context.Competitors.AddAsync(entity);
    }

    public async Task UpdateAsync(Competitor entity)
    {
        _context.Competitors.Update(entity);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Competitor entity)
    {
        _context.Competitors.Remove(entity);
        await Task.CompletedTask;
    }

    public async Task<List<Competitor>> GetAutoDetectedAsync()
    {
        return await _context.Competitors
            .Where(c => c.IsAutoDetected && !c.IsActive)
            .OrderByDescending(c => c.CreatedUtc)
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
