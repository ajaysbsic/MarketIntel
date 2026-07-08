using Alfanar.MarketIntel.Domain.Entities;

namespace Alfanar.MarketIntel.Application.Interfaces;

public interface ITenderNoticeRepository
{
    Task<TenderNotice?> GetByIdAsync(Guid id);
    Task<TenderNotice?> GetByExternalIdAsync(Guid sourceId, string externalId);
    Task<List<TenderNotice>> GetByCountryIsoAsync(string isoCode, int pageNumber = 1, int pageSize = 50);
    Task<List<TenderNotice>> GetRecentByRegionGroupAsync(string regionGroup, int take = 50);
    Task AddAsync(TenderNotice entity);
    Task UpdateAsync(TenderNotice entity);
    Task DeleteAsync(TenderNotice entity);
    Task<IEnumerable<TenderNotice>> GetBySourceNameAsync(string sourceName);
    Task DeleteRangeAsync(IEnumerable<TenderNotice> entities);
    Task SaveChangesAsync();
    // Notification inbox helpers
    Task<List<TenderNotificationLog>> GetRecentInAppLogsAsync(int pageSize = 50);
    Task<int> GetUnreadInAppCountAsync();
    Task<TenderNotificationLog?> GetNotificationLogByIdAsync(Guid id);
    Task MarkAllInAppLogsReadAsync();
    Task SaveNotificationLogAsync();
}
