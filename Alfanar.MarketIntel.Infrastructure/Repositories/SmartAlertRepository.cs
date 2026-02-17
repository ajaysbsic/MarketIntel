using Alfanar.MarketIntel.Domain.Entities;
using Alfanar.MarketIntel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alfanar.MarketIntel.Infrastructure.Repositories;

public interface ISmartAlertRepository
{
    Task<List<SmartAlert>> GetRecentAlertsAsync(int count = 10);
    Task<List<SmartAlert>> GetByCompanyAsync(string companyName);
    Task<List<SmartAlert>> GetBySeverityAsync(string severity);
    Task<List<SmartAlert>> GetByTypeAsync(string alertType);
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

    public async Task<List<SmartAlert>> GetRecentAlertsAsync(int count = 10)
    {
        // Fetch alerts from database with deduplication at database level
        var alerts = await _context.SmartAlerts
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Take(count * 3)  // Get more to account for potential duplicates
            .ToListAsync();
        
        // Deduplicate at application level by content (AlertType + Title + CompanyName)
        var uniqueAlerts = alerts
            .GroupBy(a => new { a.AlertType, a.Title, a.CompanyName })
            .Select(g => g.First())
            .OrderByDescending(a => GetAlertTypePriority(a.AlertType))
            .ThenByDescending(a => GetSeverityPriority(a.Severity))
            .ThenByDescending(a => a.CreatedAt)
            .Take(count)
            .ToList();
        
        return uniqueAlerts;
    }

    /// <summary>
    /// Get priority based on business value of alert type
    /// Higher number = higher priority
    /// </summary>
    private static int GetAlertTypePriority(string alertType)
    {
        return alertType?.ToLower() switch
        {
            // Strategic high-value events
            "mergeandacquisition" or "manda" or "m&a" => 100,
            
            // Financial health concerns
            "margindr" or "margindrop" => 90,
            "revenuedrop" or "revenues" => 85,
            
            // Investment/Financial opportunities
            "investment" or "investmentopportunity" => 80,
            "financial" or "financialhealthupdate" => 75,
            
            // Regulatory and risk events
            "regulatory" or "regulatorymention" => 70,
            "riskmention" => 65,
            "legaldispute" => 60,
            
            // Growth and opportunities
            "opportunitydetected" or "opportunity" => 55,
            "growthdetection" or "growth" => 50,
            "productlaunch" => 45,
            
            // Technology and innovation
            "technologynews" or "techupdate" => 40,
            "innovation" => 35,
            
            // General business news
            "partnership" => 30,
            "generalupdate" => 20,
            
            // Default
            _ => 10
        };
    }

    private static int GetSeverityPriority(string severity)
    {
        return severity?.ToLower() switch
        {
            "critical" => 5,
            "high" => 4,
            "medium" => 3,
            "low" => 2,
            "info" => 1,
            _ => 0
        };
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

    public async Task<List<SmartAlert>> GetByTypeAsync(string alertType)
    {
        return await _context.SmartAlerts
            .Include(a => a.FinancialReport)
            .Where(a => a.AlertType == alertType)
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
