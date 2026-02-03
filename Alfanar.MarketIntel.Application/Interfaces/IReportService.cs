using Alfanar.MarketIntel.Application.Common;
using Alfanar.MarketIntel.Application.DTOs;

namespace Alfanar.MarketIntel.Application.Interfaces;

public interface IReportService
{
    // Report ingestion and processing
    Task<Result<FinancialReportDto>> IngestReportAsync(IngestReportRequest request);
    Task<Result<FinancialReportDto>> UpdateReportAsync(Guid id, IngestReportRequest request);
    Task<Result> DeleteReportAsync(Guid id);
    
    // Report retrieval
    Task<Result<FinancialReportDto>> GetByIdAsync(Guid id, bool includeRelated = true);
    Task<Result<PaginatedList<FinancialReportDto>>> GetFilteredReportsAsync(ReportFilterDto filter);
    Task<Result<List<FinancialReportDto>>> GetRecentReportsAsync(int count = 10);
    Task<Result<List<FinancialReportDto>>> GetByCompanyAsync(string companyName);
    Task<Result<List<FinancialReportDto>>> GetPendingProcessingAsync(int maxCount = 50);
    
    // Analysis operations
    Task<Result<ReportAnalysisDto>> GetAnalysisAsync(Guid reportId);
    Task<Result<ReportAnalysisDto>> GenerateAnalysisAsync(Guid reportId);
    Task<Result<List<ReportSectionDto>>> GetSectionsAsync(Guid reportId);
    
    // Statistics and metadata
    Task<Result<List<string>>> GetDistinctCompaniesAsync();
    Task<Result<List<string>>> GetDistinctReportTypesAsync();
    Task<Result<List<string>>> GetDistinctSectorsAsync();
    Task<Result<Dictionary<string, int>>> GetReportCountByCompanyAsync();
    Task<Result<Dictionary<string, int>>> GetReportCountByStatusAsync();
    
    // File operations
    Task<Result<string>> GetFilePathAsync(Guid reportId);
    Task<Result> UpdateProcessingStatusAsync(Guid id, string status, string? errorMessage = null);
}
