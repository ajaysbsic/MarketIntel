using Alfanar.MarketIntel.Domain.Entities;
using Alfanar.MarketIntel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alfanar.MarketIntel.Infrastructure.Repositories;

public interface ISmartAlertRepository
{
    Task<List<SmartAlert>> GetRecentAlertsAsync(int count = 50);
    Task<List<SmartAlert>> GetByCompanyAsync(string companyName);
    Task<List<SmartAlert>> GetBySeverityAsync(string severity);
    Task<List<SmartAlert>> GetUnacknowledgedAsync();
    Task AddRangeAsync(List<SmartAlert> alerts);
    Task<SmartAlert?> GetByIdAsync(Guid id);
    Task UpdateAsync(SmartAlert alert);
    Task<int> SaveChangesAsync();
}

public class SmartAlertRepository : ISmartAlertRepository
{
    private readonly MarketIntelDbContext _context;

    public SmartAlertRepository(MarketIntelDbContext context)
    {
        _context = context;
    }

    public async Task<List<SmartAlert>> GetRecentAlertsAsync(int count = 50)
    {
        return await _context.SmartAlerts
            .Include(a => a.FinancialReport)
            .OrderByDescending(a => a.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<SmartAlert>> GetByCompanyAsync(string companyName)
    {
        return await _context.SmartAlerts
            .Include(a => a.FinancialReport)
            .Where(a => a.CompanyName == companyName)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<SmartAlert>> GetBySeverityAsync(string severity)
    {
        return await _context.SmartAlerts
            .Include(a => a.FinancialReport)
            .Where(a => a.Severity == severity)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<SmartAlert>> GetUnacknowledgedAsync()
    {
        return await _context.SmartAlerts
            .Include(a => a.FinancialReport)
            .Where(a => !a.IsAcknowledged)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task AddRangeAsync(List<SmartAlert> alerts)
    {
        await _context.SmartAlerts.AddRangeAsync(alerts);
    }

    public async Task<SmartAlert?> GetByIdAsync(Guid id)
    {
        return await _context.SmartAlerts
            .Include(a => a.FinancialReport)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task UpdateAsync(SmartAlert alert)
    {
        _context.SmartAlerts.Update(alert);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
