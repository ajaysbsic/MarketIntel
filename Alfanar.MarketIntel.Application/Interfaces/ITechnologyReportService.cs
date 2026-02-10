using Alfanar.MarketIntel.Application.Common;
using Alfanar.MarketIntel.Application.DTOs;

namespace Alfanar.MarketIntel.Application.Interfaces;

public interface ITechnologyReportService
{
    /// <summary>Generates a consolidated technology report for specified keywords and date range</summary>
    Task<Result<TechnologyReportDto>> GenerateReportAsync(TechnologyReportRequestDto request);

    /// <summary>Retrieves all reports with pagination</summary>
    Task<Result<PagedResultDto<TechnologyReportDto>>> GetReportsAsync(int pageNumber = 1, int pageSize = 10);

    /// <summary>Retrieves a specific report by ID</summary>
    Task<Result<TechnologyReportDto>> GetReportByIdAsync(Guid id);

    /// <summary>Gets the file path to the PDF report</summary>
    Task<Result<string>> GetReportPdfPathAsync(Guid id);

    /// <summary>Deletes a report and its associated PDF</summary>
    Task<Result<bool>> DeleteReportAsync(Guid id);

    /// <summary>Retrieves reports for a specific keyword</summary>
    Task<Result<PagedResultDto<TechnologyReportDto>>> GetReportsByKeywordAsync(string keyword, int pageNumber = 1, int pageSize = 10);
}
