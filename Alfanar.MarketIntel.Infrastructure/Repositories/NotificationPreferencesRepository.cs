using Alfanar.MarketIntel.Domain.Entities;
using Alfanar.MarketIntel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alfanar.MarketIntel.Infrastructure.Repositories;

public class NotificationPreferencesRepository : INotificationPreferencesRepository
{
    private readonly MarketIntelDbContext _context;

    public NotificationPreferencesRepository(MarketIntelDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationPreferences?> GetByUserIdAsync(string userId)
    {
        return await _context.NotificationPreferences.FirstOrDefaultAsync(p => p.UserId == userId);
    }

    public async Task<List<NotificationPreferences>> GetAllAsync()
    {
        return await _context.NotificationPreferences.ToListAsync();
    }

    public async Task UpsertAsync(NotificationPreferences preferences)
    {
        var existing = await _context.NotificationPreferences.FirstOrDefaultAsync(p => p.UserId == preferences.UserId);
        if (existing == null)
        {
            await _context.NotificationPreferences.AddAsync(preferences);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(preferences);
        }
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
