using Alfanar.MarketIntel.Domain.Entities;

namespace Alfanar.MarketIntel.Infrastructure.Repositories;

public interface IFinancialReportRepository
{
    // Basic CRUD
    Task<FinancialReport?> GetByIdAsync(Guid id, bool includeRelated = true);
    Task<FinancialReport?> GetBySourceUrlAsync(string sourceUrl);
    Task<List<FinancialReport>> GetAllAsync();
    Task AddAsync(FinancialReport report);
    Task UpdateAsync(FinancialReport report);
    Task DeleteAsync(FinancialReport report);
    Task<bool> SaveChangesAsync();
    
    // Existence checks
    Task<bool> ExistsBySourceUrlAsync(string sourceUrl);
    Task<bool> ExistsAsync(Guid id);
    
    // Filtered queries
    Task<List<FinancialReport>> GetFilteredAsync(
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
        int pageNumber = 1,
        int pageSize = 20);
    
    Task<int> GetFilteredCountAsync(
        string? companyName = null,
        string? reportType = null,
        int? fiscalYear = null,
        string? fiscalQuarter = null,
        string? sector = null,
        string? region = null,
        string? processingStatus = null,
        bool? isProcessed = null,
        DateTime? fromDate = null,
        DateTime? toDate = null);
    
    // Specific queries
    Task<List<FinancialReport>> GetRecentAsync(int count = 10);
    Task<List<FinancialReport>> GetByCompanyAsync(string companyName);
    Task<List<FinancialReport>> GetPendingProcessingAsync(int maxCount = 50);
    Task<List<FinancialReport>> GetByFiscalPeriodAsync(int year, string? quarter = null);
    
    // Statistics
    Task<List<string>> GetDistinctCompaniesAsync();
    Task<List<string>> GetDistinctReportTypesAsync();
    Task<List<string>> GetDistinctSectorsAsync();
    Task<Dictionary<string, int>> GetReportCountByCompanyAsync();
    Task<Dictionary<string, int>> GetReportCountByStatusAsync();
}
