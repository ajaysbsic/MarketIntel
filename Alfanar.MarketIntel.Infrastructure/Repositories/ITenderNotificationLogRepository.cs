using Alfanar.MarketIntel.Domain.Entities;

namespace Alfanar.MarketIntel.Application.Interfaces;

public interface ITenderNotificationLogRepository
{
    Task<bool> ExistsByDedupKeyAsync(string dedupKey);
    Task AddAsync(TenderNotificationLog entity);
    Task SaveChangesAsync();
}
