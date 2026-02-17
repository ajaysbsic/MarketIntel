using Alfanar.MarketIntel.Application.Common;
using Alfanar.MarketIntel.Application.DTOs;

namespace Alfanar.MarketIntel.Application.Interfaces;

public interface IIntelligenceReportService
{
    /// <summary>
    /// Generates a new intelligence report for a keyword based on existing search results
    /// </summary>
    Task<Result<IntelligenceReportDto>> GenerateReportAsync(GenerateIntelligenceReportRequestDto request);

    /// <summary>
    /// Retrieves a report by ID including all source articles
    /// </summary>
    Task<Result<IntelligenceReportDto>> GetReportByIdAsync(Guid id);

    /// <summary>
    /// Retrieves all reports with pagination
    /// </summary>
    Task<Result<PagedResultDto<IntelligenceReportSummaryDto>>> GetReportsAsync(int pageNumber = 1, int pageSize = 10);

    /// <summary>
    /// Retrieves reports for a specific keyword with pagination
    /// </summary>
    Task<Result<PagedResultDto<IntelligenceReportSummaryDto>>> GetReportsByKeywordAsync(string keyword, int pageNumber = 1, int pageSize = 10);

    /// <summary>
    /// Gets the file path to the PDF for a report
    /// </summary>
    Task<Result<string>> GetReportPdfPathAsync(Guid id);

    /// <summary>
    /// Downloads the PDF file for a report
    /// </summary>
    Task<Result<byte[]>> DownloadReportPdfAsync(Guid id);

    /// <summary>
    /// Deletes a report and its associated PDF
    /// </summary>
    Task<Result<bool>> DeleteReportAsync(Guid id);

    /// <summary>
    /// Gets the most recent report for a keyword
    /// </summary>
    Task<Result<IntelligenceReportDto>> GetMostRecentReportAsync(string keyword);
}
