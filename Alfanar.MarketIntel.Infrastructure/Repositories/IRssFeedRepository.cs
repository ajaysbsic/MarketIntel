using Alfanar.MarketIntel.Domain.Entities;

namespace Alfanar.MarketIntel.Infrastructure.Repositories;

public interface IRssFeedRepository
{
    Task<RssFeed?> GetByIdAsync(Guid id);
    Task<RssFeed?> GetByUrlAsync(string url);
    Task<List<RssFeed>> GetAllAsync();
    Task<List<RssFeed>> GetActiveAsync();
    Task<bool> ExistsByUrlAsync(string url);
    Task AddAsync(RssFeed feed);
    Task UpdateAsync(RssFeed feed);
    Task DeleteAsync(RssFeed feed);
    Task<int> SaveChangesAsync();
}
