using Alfanar.MarketIntel.Domain.Entities;

namespace Alfanar.MarketIntel.Application.Interfaces;

public interface ICompetitorRepository
{
    Task<Competitor?> GetByIdAsync(Guid id);
    Task<List<Competitor>> GetAllAsync(bool includeInactive = true);
    Task<Competitor?> GetByNameAsync(string name);
    Task AddAsync(Competitor entity);
    Task UpdateAsync(Competitor entity);
    Task DeleteAsync(Competitor entity);
    Task<List<Competitor>> GetAutoDetectedAsync();
    Task SaveChangesAsync();
}
