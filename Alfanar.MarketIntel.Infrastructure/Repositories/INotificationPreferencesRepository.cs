using Alfanar.MarketIntel.Domain.Entities;

namespace Alfanar.MarketIntel.Infrastructure.Repositories;

public interface INotificationPreferencesRepository
{
    Task<NotificationPreferences?> GetByUserIdAsync(string userId);
    Task<List<NotificationPreferences>> GetAllAsync();
    Task UpsertAsync(NotificationPreferences preferences);
    Task SaveChangesAsync();
}
