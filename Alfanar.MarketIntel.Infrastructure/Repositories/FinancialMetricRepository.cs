using Alfanar.MarketIntel.Domain.Entities;
using Alfanar.MarketIntel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alfanar.MarketIntel.Infrastructure.Repositories;

public interface IFinancialMetricRepository
{
    Task<List<FinancialMetric>> GetByReportIdAsync(Guid reportId);
    Task<List<FinancialMetric>> GetByCompanyAsync(string companyName, string? metricType = null);
    Task<List<FinancialMetric>> GetTimeSeriesAsync(string companyName, string metricType, DateTime? fromDate = null, DateTime? toDate = null);
    Task AddRangeAsync(List<FinancialMetric> metrics);
    Task<int> SaveChangesAsync();
}

public class FinancialMetricRepository : IFinancialMetricRepository
{
    private readonly MarketIntelDbContext _context;

    public FinancialMetricRepository(MarketIntelDbContext context)
    {
        _context = context;
    }

    public async Task<List<FinancialMetric>> GetByReportIdAsync(Guid reportId)
    {
        return await _context.FinancialMetrics
            .Where(m => m.FinancialReportId == reportId)
            .OrderBy(m => m.MetricType)
            .ToListAsync();
    }

    public async Task<List<FinancialMetric>> GetByCompanyAsync(string companyName, string? metricType = null)
    {
        var query = _context.FinancialMetrics
            .Include(m => m.FinancialReport)
            .Where(m => m.FinancialReport.CompanyName == companyName);

        if (!string.IsNullOrWhiteSpace(metricType))
        {
            query = query.Where(m => m.MetricType == metricType);
        }

        return await query
            .OrderByDescending(m => m.FinancialReport.PublishedDate)
            .ThenBy(m => m.MetricType)
            .ToListAsync();
    }

    public async Task<List<FinancialMetric>> GetTimeSeriesAsync(
        string companyName, 
        string metricType, 
        DateTime? fromDate = null, 
        DateTime? toDate = null)
    {
        var query = _context.FinancialMetrics
            .Include(m => m.FinancialReport)
            .Where(m => m.FinancialReport.CompanyName == companyName && m.MetricType == metricType);

        if (fromDate.HasValue)
        {
            query = query.Where(m => m.FinancialReport.PublishedDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(m => m.FinancialReport.PublishedDate <= toDate.Value);
        }

        return await query
            .OrderBy(m => m.FinancialReport.PublishedDate)
            .ToListAsync();
    }

    public async Task AddRangeAsync(List<FinancialMetric> metrics)
    {
        await _context.FinancialMetrics.AddRangeAsync(metrics);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
