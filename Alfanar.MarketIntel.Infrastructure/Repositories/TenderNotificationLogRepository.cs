using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Domain.Entities;
using Alfanar.MarketIntel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alfanar.MarketIntel.Infrastructure.Repositories;

public class TenderNotificationLogRepository : ITenderNotificationLogRepository
{
    private readonly MarketIntelDbContext _context;

    public TenderNotificationLogRepository(MarketIntelDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsByDedupKeyAsync(string dedupKey)
    {
        return await _context.TenderNotificationLogs.AnyAsync(x => x.DedupKey == dedupKey);
    }

    public async Task AddAsync(TenderNotificationLog entity)
    {
        await _context.TenderNotificationLogs.AddAsync(entity);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
