using Alfanar.MarketIntel.Application.DTOs;
using Alfanar.MarketIntel.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Alfanar.MarketIntel.Api.Controllers;

/// <summary>
/// API endpoints for technology reports - generation, retrieval, and PDF download
/// </summary>
[ApiController]
[Route("api/technology-reports")]
public class TechnologyReportController : ControllerBase
{
    private readonly ITechnologyReportService _service;
    private readonly ILogger<TechnologyReportController> _logger;

    public TechnologyReportController(
        ITechnologyReportService service,
        ILogger<TechnologyReportController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Generates a new technology report for the specified keywords and date range
    /// </summary>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(TechnologyReportDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateReportAsync([FromBody] TechnologyReportRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (request.Keywords == null || request.Keywords.Count == 0)
            return BadRequest(new { message = "At least one keyword is required" });

        if (request.StartDate >= request.EndDate)
            return BadRequest(new { message = "Start date must be before end date" });

        _logger.LogInformation("Generating technology report for {KeywordCount} keywords", request.Keywords.Count);
        var result = await _service.GenerateReportAsync(request);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return CreatedAtAction(nameof(GetReportByIdAsync), new { id = result.Data.Id }, result.Data);
    }

    /// <summary>
    /// Retrieves all technology reports with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<TechnologyReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReportsAsync(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        _logger.LogInformation("Retrieving technology reports (page {PageNumber})", pageNumber);
        var result = await _service.GetReportsAsync(pageNumber, pageSize);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    /// <summary>
    /// Retrieves a specific technology report by ID with all results
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TechnologyReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReportByIdAsync(Guid id)
    {
        var result = await _service.GetReportByIdAsync(id);
        
        if (!result.IsSuccess)
            return NotFound(new { message = result.Error });

        return Ok(result.Data);
    }

    /// <summary>
    /// Retrieves reports associated with a specific keyword
    /// </summary>
    [HttpGet("by-keyword/{keyword}")]
    [ProducesResponseType(typeof(PagedResultDto<TechnologyReportDto>), StatusCodes.Status200OK)]
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
    /// Gets the PDF file path for a specific report (for downloading)
    /// </summary>
    [HttpGet("{id}/pdf-path")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReportPdfPathAsync(Guid id)
    {
        var result = await _service.GetReportPdfPathAsync(id);
        
        if (!result.IsSuccess)
            return NotFound(new { message = result.Error });

        return Ok(new { pdfPath = result.Data });
    }

    /// <summary>
    /// Downloads a report as PDF (returns file stream)
    /// </summary>
    [HttpGet("{id}/download-pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadReportPdfAsync(Guid id)
    {
        var pathResult = await _service.GetReportPdfPathAsync(id);
        
        if (!pathResult.IsSuccess)
            return NotFound(new { message = pathResult.Error });

        var pdfPath = pathResult.Data;
        if (string.IsNullOrEmpty(pdfPath) || !System.IO.File.Exists(pdfPath))
            return NotFound(new { message = "PDF file not found" });

        var fileBytes = System.IO.File.ReadAllBytes(pdfPath);
        var fileName = System.IO.Path.GetFileName(pdfPath);
        
        return File(fileBytes, "application/pdf", fileName);
    }

    /// <summary>
    /// Deletes a technology report
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteReportAsync(Guid id)
    {
        _logger.LogInformation("Deleting technology report {Id}", id);
        var result = await _service.DeleteReportAsync(id);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(new { message = "Report deleted successfully" });
    }


}
