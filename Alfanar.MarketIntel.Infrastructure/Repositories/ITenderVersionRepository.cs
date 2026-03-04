using Alfanar.MarketIntel.Domain.Entities;

namespace Alfanar.MarketIntel.Application.Interfaces;

public interface ITenderVersionRepository
{
    Task<TenderVersion?> GetByIdAsync(Guid id);
    Task<List<TenderVersion>> GetByTenderNoticeIdAsync(Guid tenderNoticeId);
    Task<TenderVersion?> GetLatestByTenderNoticeIdAsync(Guid tenderNoticeId);
    Task AddAsync(TenderVersion entity);
    Task SaveChangesAsync();
}
