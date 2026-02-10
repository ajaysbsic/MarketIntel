using Alfanar.MarketIntel.Domain.Entities;
using Alfanar.MarketIntel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Alfanar.MarketIntel.Infrastructure.Repositories;

public class FinancialReportRepository : IFinancialReportRepository
{
    private readonly MarketIntelDbContext _context;
    private readonly ILogger<FinancialReportRepository> _logger;

    public FinancialReportRepository(
        MarketIntelDbContext context,
        ILogger<FinancialReportRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<FinancialReport?> GetByIdAsync(Guid id, bool includeRelated = true)
    {
        var query = _context.FinancialReports.AsQueryable();

        if (includeRelated)
        {
            query = query
                .Include(r => r.Sections.OrderBy(s => s.OrderIndex))
                .Include(r => r.Analysis)
                .Include(r => r.RelatedArticles)
                .Include(r => r.FinancialReportTags)
                .ThenInclude(rt => rt.Tag);
        }

        return await query.FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<FinancialReport?> GetBySourceUrlAsync(string sourceUrl)
    {
        return await _context.FinancialReports
            .FirstOrDefaultAsync(r => r.SourceUrl == sourceUrl);
    }

    public async Task<List<FinancialReport>> GetAllAsync()
    {
        return await _context.FinancialReports
            .OrderByDescending(r => r.PublishedDate ?? r.CreatedUtc)
            .Include(r => r.FinancialReportTags)
            .ThenInclude(rt => rt.Tag)
            .ToListAsync();
    }

    public async Task AddAsync(FinancialReport report)
    {
        await _context.FinancialReports.AddAsync(report);
    }

    public async Task UpdateAsync(FinancialReport report)
    {
        report.UpdatedUtc = DateTime.UtcNow;
        _context.FinancialReports.Update(report);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(FinancialReport report)
    {
        _context.FinancialReports.Remove(report);
        await Task.CompletedTask;
    }

    public async Task<bool> SaveChangesAsync()
    {
        try
        {
            int attempts = 0;
            const int maxAttempts = 3;
            
            while (attempts < maxAttempts)
            {
                try
                {
                    var result = await _context.SaveChangesAsync() > 0;
                    return result;
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
                {
                    attempts++;
                    if (attempts >= maxAttempts)
                    {
                        _logger.LogError(ex, "Concurrency conflict after {Attempts} attempts, clearing tracking", maxAttempts);
                        _context.ChangeTracker.Clear();
                        throw;
                    }
                    
                    _logger.LogWarning("Concurrency conflict (attempt {Attempt}/{Max}), retrying...", attempts, maxAttempts);
                    foreach (var entry in _context.ChangeTracker.Entries().Where(e => e.State != Microsoft.EntityFrameworkCore.EntityState.Unchanged).ToList())
                    {
                        await entry.ReloadAsync();
                    }
                }
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving changes to database");
            throw;
        }
    }

    public async Task<bool> ExistsBySourceUrlAsync(string sourceUrl)
    {
        return await _context.FinancialReports
            .AnyAsync(r => r.SourceUrl == sourceUrl);
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.FinancialReports
            .AnyAsync(r => r.Id == id);
    }

    public async Task<List<FinancialReport>> GetFilteredAsync(
        string? companyName = null,
        string? reportType = null,
        int? fiscalYear = null,
        string? fiscalQuarter = null,
        string? sector = null,
        string? region = null,
        string? processingStatus = null,
        bool? isProcessed = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        List<string>? tags = null,
        int pageNumber = 1,
        int pageSize = 20)
    {
        var query = _context.FinancialReports
            .Include(r => r.FinancialReportTags)
            .ThenInclude(rt => rt.Tag)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(companyName))
            query = query.Where(r => r.CompanyName.Contains(companyName));

        if (!string.IsNullOrWhiteSpace(reportType))
            query = query.Where(r => r.ReportType == reportType);

        if (fiscalYear.HasValue)
            query = query.Where(r => r.FiscalYear == fiscalYear);

        if (!string.IsNullOrWhiteSpace(fiscalQuarter))
            query = query.Where(r => r.FiscalQuarter == fiscalQuarter);

        if (!string.IsNullOrWhiteSpace(sector))
            query = query.Where(r => r.Sector == sector);

        if (!string.IsNullOrWhiteSpace(region))
            query = query.Where(r => r.Region == region);

        if (!string.IsNullOrWhiteSpace(processingStatus))
            query = query.Where(r => r.ProcessingStatus == processingStatus);

        if (isProcessed.HasValue)
            query = query.Where(r => r.IsProcessed == isProcessed);

        if (fromDate.HasValue)
            query = query.Where(r => r.PublishedDate >= fromDate || r.CreatedUtc >= fromDate);

        if (toDate.HasValue)
            query = query.Where(r => r.PublishedDate <= toDate || r.CreatedUtc <= toDate);

        if (tags != null && tags.Any())
        {
            var normalizedTags = tags.Select(t => t.ToUpperInvariant()).ToList();
            query = query.Where(r => r.FinancialReportTags
                .Any(rt => normalizedTags.Contains(rt.Tag.NormalizedName)));
        }

        // Pagination
        return await query
            .OrderByDescending(r => r.PublishedDate ?? r.CreatedUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Include(r => r.Analysis)
            .ToListAsync();
    }

    public async Task<int> GetFilteredCountAsync(
        string? companyName = null,
        string? reportType = null,
        int? fiscalYear = null,
        string? fiscalQuarter = null,
        string? sector = null,
        string? region = null,
        string? processingStatus = null,
        bool? isProcessed = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        List<string>? tags = null)
    {
        var query = _context.FinancialReports.AsQueryable();

        // Apply same filters
        if (!string.IsNullOrWhiteSpace(companyName))
            query = query.Where(r => r.CompanyName.Contains(companyName));

        if (!string.IsNullOrWhiteSpace(reportType))
            query = query.Where(r => r.ReportType == reportType);

        if (fiscalYear.HasValue)
            query = query.Where(r => r.FiscalYear == fiscalYear);

        if (!string.IsNullOrWhiteSpace(fiscalQuarter))
            query = query.Where(r => r.FiscalQuarter == fiscalQuarter);

        if (!string.IsNullOrWhiteSpace(sector))
            query = query.Where(r => r.Sector == sector);

        if (!string.IsNullOrWhiteSpace(region))
            query = query.Where(r => r.Region == region);

        if (!string.IsNullOrWhiteSpace(processingStatus))
            query = query.Where(r => r.ProcessingStatus == processingStatus);

        if (isProcessed.HasValue)
            query = query.Where(r => r.IsProcessed == isProcessed);

        if (fromDate.HasValue)
            query = query.Where(r => r.PublishedDate >= fromDate || r.CreatedUtc >= fromDate);

        if (toDate.HasValue)
            query = query.Where(r => r.PublishedDate <= toDate || r.CreatedUtc <= toDate);

        if (tags != null && tags.Any())
        {
            var normalizedTags = tags.Select(t => t.ToUpperInvariant()).ToList();
            query = query.Where(r => r.FinancialReportTags
                .Any(rt => normalizedTags.Contains(rt.Tag.NormalizedName)));
        }

        return await query.CountAsync();
    }

    public async Task<List<FinancialReport>> GetRecentAsync(int count = 10)
    {
        return await _context.FinancialReports
            .OrderByDescending(r => r.PublishedDate ?? r.CreatedUtc)
            .Take(count)
            .Include(r => r.Analysis)
            .Include(r => r.FinancialReportTags)
            .ThenInclude(rt => rt.Tag)
            .ToListAsync();
    }

    public async Task<List<FinancialReport>> GetByCompanyAsync(string companyName)
    {
        return await _context.FinancialReports
            .Where(r => r.CompanyName.Contains(companyName))
            .OrderByDescending(r => r.PublishedDate ?? r.CreatedUtc)
            .Include(r => r.Analysis)
            .Include(r => r.FinancialReportTags)
            .ThenInclude(rt => rt.Tag)
            .ToListAsync();
    }

    public async Task<List<FinancialReport>> GetPendingProcessingAsync(int maxCount = 50)
    {
        return await _context.FinancialReports
            .Where(r => !r.IsProcessed && r.ProcessingStatus != "Failed")
            .OrderBy(r => r.CreatedUtc)
            .Take(maxCount)
            .ToListAsync();
    }

    public async Task<List<FinancialReport>> GetByFiscalPeriodAsync(int year, string? quarter = null)
    {
        var query = _context.FinancialReports
            .Where(r => r.FiscalYear == year);

        if (!string.IsNullOrWhiteSpace(quarter))
            query = query.Where(r => r.FiscalQuarter == quarter);

        return await query
            .OrderBy(r => r.CompanyName)
            .Include(r => r.Analysis)
            .ToListAsync();
    }

    public async Task<List<string>> GetDistinctCompaniesAsync()
    {
        return await _context.FinancialReports
            .Select(r => r.CompanyName)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }

    public async Task<List<string>> GetDistinctReportTypesAsync()
    {
        return await _context.FinancialReports
            .Select(r => r.ReportType)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();
    }

    public async Task<List<string>> GetDistinctSectorsAsync()
    {
        return await _context.FinancialReports
            .Where(r => r.Sector != null)
            .Select(r => r.Sector!)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync();
    }

    public async Task<Dictionary<string, int>> GetReportCountByCompanyAsync()
    {
        return await _context.FinancialReports
            .GroupBy(r => r.CompanyName)
            .Select(g => new { Company = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToDictionaryAsync(x => x.Company, x => x.Count);
    }

    public async Task<Dictionary<string, int>> GetReportCountByStatusAsync()
    {
        return await _context.FinancialReports
            .GroupBy(r => r.ProcessingStatus)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);
    }
}
