using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Domain.Entities;
using Alfanar.MarketIntel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alfanar.MarketIntel.Infrastructure.Repositories;

public class TenderNoticeRepository : ITenderNoticeRepository
{
    private readonly MarketIntelDbContext _context;

    public TenderNoticeRepository(MarketIntelDbContext context)
    {
        _context = context;
    }

    public async Task<TenderNotice?> GetByIdAsync(Guid id)
    {
        return await _context.TenderNotices
            .Include(x => x.Source)
            .Include(x => x.Authority)
            .Include(x => x.Country)
            .Include(x => x.CurrentVersion)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<TenderNotice?> GetByExternalIdAsync(Guid sourceId, string externalId)
    {
        return await _context.TenderNotices
            .Include(x => x.CurrentVersion)
            .FirstOrDefaultAsync(x => x.SourceId == sourceId && x.ExternalId == externalId);
    }

    public async Task<List<TenderNotice>> GetByCountryIsoAsync(string isoCode, int pageNumber = 1, int pageSize = 50)
    {
        return await _context.TenderNotices
            .Include(x => x.Source)
            .Include(x => x.Authority)
            .Include(x => x.Country)
            .Where(x => x.Country.IsoCode == isoCode)
            .OrderByDescending(x => x.PublishDate)
            .ThenByDescending(x => x.LastChangedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<TenderNotice>> GetRecentByRegionGroupAsync(string regionGroup, int take = 50)
    {
        return await _context.TenderNotices
            .Include(x => x.Source)
            .Include(x => x.Authority)
            .Include(x => x.Country)
            .Where(x => x.Country.RegionGroup == regionGroup && x.IsActive)
            .OrderByDescending(x => x.PublishDate)
            .ThenByDescending(x => x.LastChangedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task AddAsync(TenderNotice entity)
    {
        await _context.TenderNotices.AddAsync(entity);
    }

    public async Task UpdateAsync(TenderNotice entity)
    {
        _context.TenderNotices.Update(entity);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(TenderNotice entity)
    {
        _context.TenderNotices.Remove(entity);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
