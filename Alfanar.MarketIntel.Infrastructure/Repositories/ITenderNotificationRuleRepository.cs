using Alfanar.MarketIntel.Domain.Entities;

namespace Alfanar.MarketIntel.Application.Interfaces;

public interface ITenderNotificationRuleRepository
{
    Task<TenderNotificationRule?> GetByIdAsync(Guid id);
    Task<List<TenderNotificationRule>> GetActiveRulesAsync();
    Task<List<TenderNotificationRule>> GetActiveRulesForUserAsync(string userId);
    Task AddAsync(TenderNotificationRule entity);
    Task UpdateAsync(TenderNotificationRule entity);
    Task DeleteAsync(TenderNotificationRule entity);
    Task SaveChangesAsync();
}
