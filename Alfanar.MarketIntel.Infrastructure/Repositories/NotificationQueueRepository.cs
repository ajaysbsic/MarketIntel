using Alfanar.MarketIntel.Domain.Entities;
using Alfanar.MarketIntel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alfanar.MarketIntel.Infrastructure.Repositories;

public class NotificationQueueRepository : INotificationQueueRepository
{
    private readonly MarketIntelDbContext _context;

    public NotificationQueueRepository(MarketIntelDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationQueue?> GetByIdAsync(Guid id)
    {
        return await _context.NotificationQueues.FirstOrDefaultAsync(n => n.Id == id);
    }

    public async Task<List<NotificationQueue>> GetPendingAsync(int batchSize = 100)
    {
        return await _context.NotificationQueues
            .Where(n => n.Status == NotificationStatus.Pending || n.Status == NotificationStatus.Processing)
            .OrderBy(n => n.CreatedAt)
            .Take(batchSize)
            .ToListAsync();
    }

    public async Task AddAsync(NotificationQueue queueItem)
    {
        await _context.NotificationQueues.AddAsync(queueItem);
    }

    public async Task UpdateAsync(NotificationQueue queueItem)
    {
        _context.NotificationQueues.Update(queueItem);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
