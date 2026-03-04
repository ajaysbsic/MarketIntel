using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Domain.Entities;
using Alfanar.MarketIntel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alfanar.MarketIntel.Infrastructure.Repositories;

public class TenderNotificationRuleRepository : ITenderNotificationRuleRepository
{
    private readonly MarketIntelDbContext _context;

    public TenderNotificationRuleRepository(MarketIntelDbContext context)
    {
        _context = context;
    }

    public async Task<TenderNotificationRule?> GetByIdAsync(Guid id)
    {
        return await _context.TenderNotificationRules.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<List<TenderNotificationRule>> GetActiveRulesAsync()
    {
        return await _context.TenderNotificationRules
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.CreatedUtc)
            .ToListAsync();
    }

    public async Task<List<TenderNotificationRule>> GetActiveRulesForUserAsync(string userId)
    {
        return await _context.TenderNotificationRules
            .Where(x => x.IsActive && (x.Scope == "Global" || x.UserId == userId))
            .OrderByDescending(x => x.CreatedUtc)
            .ToListAsync();
    }

    public async Task AddAsync(TenderNotificationRule entity)
    {
        await _context.TenderNotificationRules.AddAsync(entity);
    }

    public async Task UpdateAsync(TenderNotificationRule entity)
    {
        _context.TenderNotificationRules.Update(entity);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(TenderNotificationRule entity)
    {
        _context.TenderNotificationRules.Remove(entity);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
