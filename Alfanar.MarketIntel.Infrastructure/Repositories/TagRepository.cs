using Alfanar.MarketIntel.Domain.Entities;
using Alfanar.MarketIntel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alfanar.MarketIntel.Infrastructure.Repositories;

public class TagRepository : ITagRepository
{
    private readonly MarketIntelDbContext _context;

    public TagRepository(MarketIntelDbContext context)
    {
        _context = context;
    }

    public async Task<Tag?> GetByIdAsync(int id)
    {
        return await _context.Tags.FindAsync(id);
    }

    public async Task<Tag?> GetByNameAsync(string name)
    {
        var normalizedName = name.ToUpperInvariant();
        return await _context.Tags
            .FirstOrDefaultAsync(t => t.NormalizedName == normalizedName);
    }

    public async Task<List<Tag>> GetAllAsync()
    {
        return await _context.Tags
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<List<Tag>> GetByNamesAsync(List<string> names)
    {
        var normalizedNames = names.Select(n => n.ToUpperInvariant()).ToList();
        return await _context.Tags
            .Where(t => normalizedNames.Contains(t.NormalizedName))
            .ToListAsync();
    }

    public async Task<Tag> GetOrCreateAsync(string tagName)
    {
        var normalizedName = tagName.ToUpperInvariant();
        var tag = await _context.Tags
            .FirstOrDefaultAsync(t => t.NormalizedName == normalizedName);

        if (tag == null)
        {
            tag = new Tag
            {
                Name = tagName,
                NormalizedName = normalizedName,
                CreatedUtc = DateTime.UtcNow
            };
            await _context.Tags.AddAsync(tag);
            await _context.SaveChangesAsync();
        }

        return tag;
    }

    public async Task AddAsync(Tag tag)
    {
        await _context.Tags.AddAsync(tag);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
