using Alfanar.MarketIntel.Application.Common;
using Alfanar.MarketIntel.Application.DTOs;
using Alfanar.MarketIntel.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Alfanar.MarketIntel.Api.Controllers;

/// <summary>
/// API endpoints for AI-powered intelligence reports
/// </summary>
[ApiController]
[Route("api/intelligence-reports")]
public class IntelligenceReportController : ControllerBase
{
    private readonly IIntelligenceReportService _service;
    private readonly ILogger<IntelligenceReportController> _logger;

    public IntelligenceReportController(
        IIntelligenceReportService service,
        ILogger<IntelligenceReportController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Generates a new intelligence report for a keyword based on existing search results
    /// </summary>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(IntelligenceReportDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateReportAsync([FromBody] GenerateIntelligenceReportRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (string.IsNullOrWhiteSpace(request.Keyword))
            return BadRequest(new { message = "Keyword is required" });

        _logger.LogInformation("Generating intelligence report for keyword: {Keyword}", request.Keyword);
        var result = await _service.GenerateReportAsync(request);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return StatusCode(StatusCodes.Status201Created, result.Data);
    }

    /// <summary>
    /// Retrieves all intelligence reports with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<IntelligenceReportSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReportsAsync(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        _logger.LogInformation("Retrieving intelligence reports (page {PageNumber})", pageNumber);
        var result = await _service.GetReportsAsync(pageNumber, pageSize);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    /// <summary>
    /// Retrieves a specific intelligence report by ID with all source articles
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(IntelligenceReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReportByIdAsync(Guid id)
    {
        var result = await _service.GetReportByIdAsync(id);

        if (!result.IsSuccess)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    /// <summary>
    /// Retrieves reports for a specific keyword
    /// </summary>
    [HttpGet("by-keyword/{keyword}")]
    [ProducesResponseType(typeof(PagedResultDto<IntelligenceReportSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReportsByKeywordAsync(
        string keyword,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return BadRequest(new { message = "Keyword cannot be empty" });

        _logger.LogInformation("Retrieving reports for keyword: {Keyword}", keyword);
        var result = await _service.GetReportsByKeywordAsync(keyword, pageNumber, pageSize);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    /// <summary>
    /// Gets the most recent intelligence report for a keyword
    /// </summary>
    [HttpGet("most-recent/{keyword}")]
    [ProducesResponseType(typeof(IntelligenceReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMostRecentReportAsync(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return BadRequest(new { message = "Keyword cannot be empty" });

        var result = await _service.GetMostRecentReportAsync(keyword);

        if (!result.IsSuccess)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    /// <summary>
    /// Downloads the PDF for an intelligence report
    /// </summary>
    [HttpGet("{id}/download-pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadPdfAsync(Guid id)
    {
        var result = await _service.DownloadReportPdfAsync(id);

        if (!result.IsSuccess)
            return NotFound(new { message = result.Error });

        return File(result.Data, "application/pdf", $"intelligence-report-{id}.pdf");
    }

    /// <summary>
    /// Deletes an intelligence report and its associated PDF
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteReportAsync(Guid id)
    {
        _logger.LogInformation("Deleting intelligence report: {ReportId}", id);
        var result = await _service.DeleteReportAsync(id);

        if (!result.IsSuccess)
            return NotFound(new { message = result.Error });

        return NoContent();
    }
}
