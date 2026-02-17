using Alfanar.MarketIntel.Domain.Entities;

namespace Alfanar.MarketIntel.Infrastructure.Repositories;

public interface INotificationQueueRepository
{
    Task<NotificationQueue?> GetByIdAsync(Guid id);
    Task<List<NotificationQueue>> GetPendingAsync(int batchSize = 100);
    Task AddAsync(NotificationQueue queueItem);
    Task UpdateAsync(NotificationQueue queueItem);
    Task SaveChangesAsync();
}
