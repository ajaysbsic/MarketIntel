using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Domain.Entities;
using Alfanar.MarketIntel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alfanar.MarketIntel.Infrastructure.Repositories;

public class TechnologyReportRepository : ITechnologyReportRepository
{
    private readonly MarketIntelDbContext _context;

    public TechnologyReportRepository(MarketIntelDbContext context)
    {
        _context = context;
    }

    public async Task<TechnologyReport?> GetByIdAsync(Guid id)
    {
        return await _context.TechnologyReports.FindAsync(id);
    }

    public async Task<List<TechnologyReport>> GetAllAsync()
    {
        return await _context.TechnologyReports.ToListAsync();
    }

    public async Task AddAsync(TechnologyReport entity)
    {
        await _context.TechnologyReports.AddAsync(entity);
    }

    public async Task UpdateAsync(TechnologyReport entity)
    {
        _context.TechnologyReports.Update(entity);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(TechnologyReport entity)
    {
        _context.TechnologyReports.Remove(entity);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<TechnologyReport?> GetByIdWithResultsAsync(Guid id)
    {
        return await _context.TechnologyReports
            .Include(r => r.ReportResults)
            .ThenInclude(rr => rr.WebSearchResult)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<List<TechnologyReport>> GetReportsAsync(int pageNumber = 1, int pageSize = 10)
    {
        return await _context.TechnologyReports
            .OrderByDescending(r => r.GeneratedUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<TechnologyReport>> GetReportsForKeywordAsync(string keyword, int pageNumber = 1, int pageSize = 10)
    {
        return await _context.TechnologyReports
            .Where(r => r.Keywords.Contains(keyword))
            .OrderByDescending(r => r.GeneratedUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<TechnologyReport>> GetReportsForDateRangeAsync(DateTime startDate, DateTime endDate, int pageNumber = 1, int pageSize = 10)
    {
        return await _context.TechnologyReports
            .Where(r => r.GeneratedUtc >= startDate && r.GeneratedUtc <= endDate)
            .OrderByDescending(r => r.GeneratedUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<int> GetReportsCountAsync()
    {
        return await _context.TechnologyReports
            .CountAsync();
    }
}
