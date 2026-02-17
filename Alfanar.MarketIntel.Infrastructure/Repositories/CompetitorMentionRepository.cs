using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Domain.Entities;
using Alfanar.MarketIntel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alfanar.MarketIntel.Infrastructure.Repositories;

public class CompetitorMentionRepository : ICompetitorMentionRepository
{
    private readonly MarketIntelDbContext _context;

    public CompetitorMentionRepository(MarketIntelDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(CompetitorMention entity)
    {
        await _context.CompetitorMentions.AddAsync(entity);
    }

    public async Task AddRangeAsync(List<CompetitorMention> entities)
    {
        await _context.CompetitorMentions.AddRangeAsync(entities);
    }

    public async Task<List<CompetitorMention>> GetByCompetitorAsync(Guid competitorId)
    {
        return await _context.CompetitorMentions
            .Where(m => m.CompetitorId == competitorId)
            .OrderByDescending(m => m.DetectedUtc)
            .ToListAsync();
    }

    public async Task<List<CompetitorMention>> GetRecentByCompetitorAsync(Guid competitorId, int count = 50)
    {
        return await _context.CompetitorMentions
            .Where(m => m.CompetitorId == competitorId)
            .OrderByDescending(m => m.DetectedUtc)
            .Take(count)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(Guid competitorId, string sourceType, Guid sourceId)
    {
        return await _context.CompetitorMentions.AnyAsync(m =>
            m.CompetitorId == competitorId &&
            m.SourceType == sourceType &&
            m.SourceId == sourceId);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
