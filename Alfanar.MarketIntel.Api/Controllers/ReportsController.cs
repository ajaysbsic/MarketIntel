using Alfanar.MarketIntel.Api.Hubs;
using Alfanar.MarketIntel.Application.DTOs;
using Alfanar.MarketIntel.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Alfanar.MarketIntel.Api.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly IFileStorageService _fileStorage;
    private readonly IHubContext<NotificationsHub> _hub;
    private readonly IValidator<IngestReportRequest> _validator;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IReportService reportService,
        IFileStorageService fileStorage,
        IHubContext<NotificationsHub> hub,
        IValidator<IngestReportRequest> validator,
        ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _fileStorage = fileStorage;
        _hub = hub;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>
    /// Ingest a new financial report
    /// </summary>
    [HttpPost("ingest")]
    [ProducesResponseType(typeof(FinancialReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> IngestReport([FromBody] IngestReportRequest request)
    {
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(new { errors });
        }

        var result = await _reportService.IngestReportAsync(request);

        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("already exists") == true)
                return Conflict(new { message = result.Error });

            return BadRequest(new { message = result.Error });
        }

        // Send real-time notification
        await _hub.Clients.All.SendAsync("newReport", new
        {
            result.Data!.Id,
            result.Data.CompanyName,
            result.Data.ReportType,
            result.Data.Title,
            result.Data.SourceUrl,
            result.Data.FiscalYear,
            result.Data.FiscalQuarter,
            result.Data.PublishedDate
        });

        return Ok(result.Data);
    }

    /// <summary>
    /// Get a specific financial report by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FinancialReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, [FromQuery] bool includeRelated = true)
    {
        var result = await _reportService.GetByIdAsync(id, includeRelated);

        if (!result.IsSuccess)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    /// <summary>
    /// Get filtered and paginated financial reports
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFiltered([FromQuery] ReportFilterDto filter)
    {
        var result = await _reportService.GetFilteredReportsAsync(filter);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(new
        {
            items = result.Data!.Items,
            pageNumber = result.Data.PageNumber,
            pageSize = result.Data.PageSize,
            totalPages = result.Data.TotalPages,
            totalCount = result.Data.TotalCount,
            hasPreviousPage = result.Data.HasPreviousPage,
            hasNextPage = result.Data.HasNextPage
        });
    }

    /// <summary>
    /// Get most recent financial reports
    /// </summary>
    [HttpGet("recent")]
    [ProducesResponseType(typeof(List<FinancialReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecent([FromQuery] int count = 10)
    {
        if (count < 1 || count > 100)
            return BadRequest(new { message = "Count must be between 1 and 100" });

        var result = await _reportService.GetRecentReportsAsync(count);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    /// <summary>
    /// Get reports by company name
    /// </summary>
    [HttpGet("company/{companyName}")]
    [ProducesResponseType(typeof(List<FinancialReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCompany(string companyName)
    {
        var result = await _reportService.GetByCompanyAsync(companyName);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    /// <summary>
    /// Get pending processing reports
    /// </summary>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(List<FinancialReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPending([FromQuery] int maxCount = 50)
    {
        var result = await _reportService.GetPendingProcessingAsync(maxCount);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    /// <summary>
    /// Get report analysis
    /// </summary>
    [HttpGet("{id:guid}/analysis")]
    [ProducesResponseType(typeof(ReportAnalysisDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAnalysis(Guid id)
    {
        var result = await _reportService.GetAnalysisAsync(id);

        if (!result.IsSuccess)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    /// <summary>
    /// Generate analysis for a report
    /// </summary>
    [HttpPost("{id:guid}/analyze")]
    [ProducesResponseType(typeof(ReportAnalysisDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateAnalysis(Guid id)
    {
        var result = await _reportService.GenerateAnalysisAsync(id);

        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("not found") == true)
                return NotFound(new { message = result.Error });

            return BadRequest(new { message = result.Error });
        }

        // Send real-time notification
        await _hub.Clients.All.SendAsync("reportAnalysisComplete", new
        {
            reportId = id,
            analysis = result.Data
        });

        return Ok(result.Data);
    }

    /// <summary>
    /// Get report sections
    /// </summary>
    [HttpGet("{id:guid}/sections")]
    [ProducesResponseType(typeof(List<ReportSectionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSections(Guid id)
    {
        var result = await _reportService.GetSectionsAsync(id);

        if (!result.IsSuccess)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    /// <summary>
    /// Download report PDF file
    /// </summary>
    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadReport(Guid id)
    {
        try
        {
            _logger.LogInformation("Download request for report {ReportId}", id);

            var filePathResult = await _reportService.GetFilePathAsync(id);

            if (!filePathResult.IsSuccess)
            {
                _logger.LogWarning("File path not found for report {ReportId}: {Error}", id, filePathResult.Error);
                return NotFound(new { message = filePathResult.Error });
            }

            var filePath = filePathResult.Data!;
            _logger.LogInformation("Retrieved file path for report {ReportId}: {FilePath}", id, filePath);

            var fileStreamResult = await _fileStorage.GetFileStreamAsync(filePath);

            if (!fileStreamResult.IsSuccess || fileStreamResult.Data == null)
            {
                _logger.LogError("Failed to retrieve file for report {ReportId}: {Error}", id, fileStreamResult.Error);
                return NotFound(new { message = fileStreamResult.Error });
            }

            var fileName = Path.GetFileName(filePath);
            _logger.LogInformation("Returning file {FileName} for report {ReportId}", fileName, id);
            
            return File(fileStreamResult.Data, "application/pdf", fileName, enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during download for report {ReportId}", id);
            return StatusCode(500, new { message = "An error occurred while downloading the file" });
        }
    }

    /// <summary>
    /// Update report
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(FinancialReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateReport(Guid id, [FromBody] IngestReportRequest request)
    {
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(new { errors });
        }

        var result = await _reportService.UpdateReportAsync(id, request);

        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("not found") == true)
                return NotFound(new { message = result.Error });

            return BadRequest(new { message = result.Error });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Delete a financial report
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteReport(Guid id)
    {
        var result = await _reportService.DeleteReportAsync(id);

        if (!result.IsSuccess)
            return NotFound(new { message = result.Error });

        return NoContent();
    }

    /// <summary>
    /// Get all distinct companies
    /// </summary>
    [HttpGet("companies")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCompanies()
    {
        var result = await _reportService.GetDistinctCompaniesAsync();

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    /// <summary>
    /// Get all distinct report types
    /// </summary>
    [HttpGet("types")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTypes()
    {
        var result = await _reportService.GetDistinctReportTypesAsync();

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    /// <summary>
    /// Get all distinct sectors
    /// </summary>
    [HttpGet("sectors")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSectors()
    {
        var result = await _reportService.GetDistinctSectorsAsync();

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    /// <summary>
    /// Get report count by company
    /// </summary>
    [HttpGet("stats/by-company")]
    [ProducesResponseType(typeof(Dictionary<string, int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatsByCompany()
    {
        var result = await _reportService.GetReportCountByCompanyAsync();

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    /// <summary>
    /// Get report count by processing status
    /// </summary>
    [HttpGet("stats/by-status")]
    [ProducesResponseType(typeof(Dictionary<string, int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatsByStatus()
    {
        var result = await _reportService.GetReportCountByStatusAsync();

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    /// <summary>
    /// Batch generate analysis for reports that have extracted text but no analysis
    /// </summary>
    [HttpPost("batch-analyze")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BatchAnalyzeReports([FromQuery] int maxCount = 50)
    {
        try
        {
            _logger.LogInformation("Batch analysis requested for up to {MaxCount} reports", maxCount);

            // Get reports that have extracted text but no analysis
            var pendingReports = await _reportService.GetPendingProcessingAsync(maxCount);

            if (!pendingReports.IsSuccess || pendingReports.Data?.Count == 0)
            {
                _logger.LogInformation("No pending reports found for analysis");
                return Ok(new { message = "No pending reports", analyzed = 0 });
            }

            var reports = pendingReports.Data!;
            _logger.LogInformation("Found {Count} reports pending analysis", reports.Count);

            int analyzedCount = 0;
            var errors = new List<string>();

            foreach (var report in reports)
            {
                try
                {
                    _logger.LogInformation("Generating analysis for report {Id}: {Title}", report.Id, report.Title);
                    
                    var analysisResult = await _reportService.GenerateAnalysisAsync(report.Id);

                    if (analysisResult.IsSuccess)
                    {
                        analyzedCount++;
                        _logger.LogInformation("? Analysis complete for {Title}", report.Title);

                        // Send real-time notification
                        await _hub.Clients.All.SendAsync("reportAnalysisComplete", new
                        {
                            reportId = report.Id,
                            analysis = analysisResult.Data
                        });
                    }
                    else
                    {
                        errors.Add($"{report.Title}: {analysisResult.Error}");
                        _logger.LogError("Failed to analyze {Title}: {Error}", report.Title, analysisResult.Error);
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"{report.Title}: {ex.Message}");
                    _logger.LogError(ex, "Exception analyzing {Title}", report.Title);
                }

                // Delay between API calls
                await Task.Delay(2000);
            }

            return Ok(new
            {
                message = $"Batch analysis complete",
                totalProcessed = reports.Count,
                analyzed = analyzedCount,
                failed = reports.Count - analyzedCount,
                errors = errors.Any() ? errors : null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during batch analysis");
            return StatusCode(500, new { message = "An error occurred during batch analysis" });
        }
    }
}
