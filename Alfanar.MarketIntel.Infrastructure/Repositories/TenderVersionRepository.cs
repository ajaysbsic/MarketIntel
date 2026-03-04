using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Domain.Entities;
using Alfanar.MarketIntel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alfanar.MarketIntel.Infrastructure.Repositories;

public class TenderVersionRepository : ITenderVersionRepository
{
    private readonly MarketIntelDbContext _context;

    public TenderVersionRepository(MarketIntelDbContext context)
    {
        _context = context;
    }

    public async Task<TenderVersion?> GetByIdAsync(Guid id)
    {
        return await _context.TenderVersions.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<List<TenderVersion>> GetByTenderNoticeIdAsync(Guid tenderNoticeId)
    {
        return await _context.TenderVersions
            .Where(x => x.TenderNoticeId == tenderNoticeId)
            .OrderByDescending(x => x.VersionNo)
            .ToListAsync();
    }

    public async Task<TenderVersion?> GetLatestByTenderNoticeIdAsync(Guid tenderNoticeId)
    {
        return await _context.TenderVersions
            .Where(x => x.TenderNoticeId == tenderNoticeId)
            .OrderByDescending(x => x.VersionNo)
            .FirstOrDefaultAsync();
    }

    public async Task AddAsync(TenderVersion entity)
    {
        await _context.TenderVersions.AddAsync(entity);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
