using Alfanar.MarketIntel.Domain.Entities;

namespace Alfanar.MarketIntel.Application.Interfaces;

public interface ITenderSourceRepository
{
    Task<TenderSource?> GetByIdAsync(Guid id);
    Task<TenderSource?> GetByNameAsync(string name);
    Task<List<TenderSource>> GetAllAsync(bool includeDisabled = true);
    Task<List<TenderSource>> GetEnabledAsync();
    Task AddAsync(TenderSource entity);
    Task UpdateAsync(TenderSource entity);
    Task DeleteAsync(TenderSource entity);
    Task SaveChangesAsync();
}
