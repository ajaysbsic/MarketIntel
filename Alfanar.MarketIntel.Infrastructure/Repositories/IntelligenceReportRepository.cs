using Alfanar.MarketIntel.Domain.Entities;
using Alfanar.MarketIntel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alfanar.MarketIntel.Infrastructure.Repositories;

public class IntelligenceReportRepository : IIntelligenceReportRepository
{
    private readonly MarketIntelDbContext _context;

    public IntelligenceReportRepository(MarketIntelDbContext context)
    {
        _context = context;
    }

    public async Task<IntelligenceReport?> GetByIdAsync(Guid id)
    {
        return await _context.IntelligenceReports.FindAsync(id);
    }

    public async Task<List<IntelligenceReport>> GetAllAsync()
    {
        return await _context.IntelligenceReports
            .OrderByDescending(r => r.GeneratedUtc)
            .ToListAsync();
    }

    public async Task AddAsync(IntelligenceReport entity)
    {
        await _context.IntelligenceReports.AddAsync(entity);
    }

    public async Task UpdateAsync(IntelligenceReport entity)
    {
        _context.IntelligenceReports.Update(entity);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(IntelligenceReport entity)
    {
        _context.IntelligenceReports.Remove(entity);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<IntelligenceReport?> GetByIdWithResultsAsync(Guid id)
    {
        return await _context.IntelligenceReports
            .Include(r => r.ReportResults)
            .ThenInclude(rr => rr.WebSearchResult)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<List<IntelligenceReport>> GetReportsAsync(int pageNumber = 1, int pageSize = 10)
    {
        return await _context.IntelligenceReports
            .OrderByDescending(r => r.GeneratedUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<IntelligenceReport>> GetReportsByKeywordAsync(string keyword, int pageNumber = 1, int pageSize = 10)
    {
        var normalizedKeyword = NormalizeKeyword(keyword);

        var reports = await _context.IntelligenceReports
            .OrderByDescending(r => r.GeneratedUtc)
            .AsNoTracking()
            .ToListAsync();

        return reports
            .Where(r => NormalizeKeyword(r.Keyword) == normalizedKeyword)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    public async Task<List<IntelligenceReport>> GetReportsByStatusAsync(string status, int pageNumber = 1, int pageSize = 10)
    {
        return await _context.IntelligenceReports
            .Where(r => r.Status == status)
            .OrderByDescending(r => r.GeneratedUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IntelligenceReport?> GetMostRecentForKeywordAsync(string keyword)
    {
        var normalizedKeyword = NormalizeKeyword(keyword);

        var reports = await _context.IntelligenceReports
            .AsNoTracking()
            .OrderByDescending(r => r.GeneratedUtc)
            .ToListAsync();

        return reports.FirstOrDefault(r => NormalizeKeyword(r.Keyword) == normalizedKeyword);
    }

    public async Task<int> GetReportsCountAsync()
    {
        return await _context.IntelligenceReports.CountAsync();
    }

    public async Task<int> GetReportsCountByKeywordAsync(string keyword)
    {
        var normalizedKeyword = NormalizeKeyword(keyword);

        var reports = await _context.IntelligenceReports
            .AsNoTracking()
            .ToListAsync();

        return reports.Count(r => NormalizeKeyword(r.Keyword) == normalizedKeyword);
    }

    private static string NormalizeKeyword(string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return string.Empty;

        return string.Join(' ', keyword
            .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            .ToLowerInvariant();
    }
}
