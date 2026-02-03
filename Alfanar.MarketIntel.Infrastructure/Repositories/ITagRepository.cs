using Alfanar.MarketIntel.Domain.Entities;

namespace Alfanar.MarketIntel.Infrastructure.Repositories;

public interface ITagRepository
{
    Task<Tag?> GetByIdAsync(int id);
    Task<Tag?> GetByNameAsync(string name);
    Task<List<Tag>> GetAllAsync();
    Task<List<Tag>> GetByNamesAsync(List<string> names);
    Task<Tag> GetOrCreateAsync(string tagName);
    Task AddAsync(Tag tag);
    Task<int> SaveChangesAsync();
}
