using Alfanar.MarketIntel.Application.Common;
using Alfanar.MarketIntel.Application.DTOs;
using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Alfanar.MarketIntel.Application.Services;

/// <summary>
/// Service for generating consolidated technology reports from web search results
/// </summary>
public class TechnologyReportService : ITechnologyReportService
{
    private readonly ITechnologyReportRepository _reportRepository;
    private readonly IWebSearchResultRepository _searchRepository;
    private readonly IWebSearchService _webSearchService;
    private readonly ILogger<TechnologyReportService> _logger;
    private readonly MarketIntelDbContext _context;

    public TechnologyReportService(
        ITechnologyReportRepository reportRepository,
        IWebSearchResultRepository searchRepository,
        IWebSearchService webSearchService,
        ILogger<TechnologyReportService> logger,
        MarketIntelDbContext context)
    {
        _reportRepository = reportRepository;
        _searchRepository = searchRepository;
        _webSearchService = webSearchService;
        _logger = logger;
        _context = context;
    }

    public async Task<Result<TechnologyReportDto>> GenerateReportAsync(TechnologyReportRequestDto request)
    {
        try
        {
            // Validate input
            if (request.Keywords == null || !request.Keywords.Any())
                return Result<TechnologyReportDto>.Failure("At least one keyword is required");

            if (request.StartDate >= request.EndDate)
                return Result<TechnologyReportDto>.Failure("Start date must be before end date");

            // Generate title if not provided
            var title = request.Title ?? $"Technology Report: {string.Join(", ", request.Keywords.Take(3))}";

            // Aggregate results for all keywords
            var allResults = new List<Domain.Entities.WebSearchResult>();

            foreach (var keyword in request.Keywords)
            {
                var results = await _searchRepository.GetResultsByKeywordAndDateRangeAsync(
                    keyword, request.StartDate, request.EndDate);
                allResults.AddRange(results);
            }

            // Create report entity
            var report = new Domain.Entities.TechnologyReport
            {
                Title = title,
                Keywords = System.Text.Json.JsonSerializer.Serialize(request.Keywords),
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                GeneratedUtc = DateTime.UtcNow,
                TotalResults = allResults.Count
            };

            // Add results to report
            foreach (var result in allResults.DistinctBy(r => r.Url)) // Deduplicate by URL
            {
                report.ReportResults.Add(new Domain.Entities.ReportResult
                {
                    ReportId = report.Id,
                    WebSearchResultId = result.Id
                });
            }

            await _reportRepository.AddAsync(report);
            await _reportRepository.SaveChangesAsync();

            _logger.LogInformation("Generated report: {Title} with {Count} results", report.Title, allResults.Count);

            // TODO: Generate PDF (Phase will be added later in implementation)
            // var pdfPath = await _pdfGenerator.GeneratePdfAsync(report);
            // report.PdfFilePath = pdfPath;
            // await _reportRepository.UpdateAsync(report);
            // await _reportRepository.SaveChangesAsync();

            return Result<TechnologyReportDto>.Success(await MapToDtoAsync(report));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating technology report");
            return Result<TechnologyReportDto>.Failure($"Error generating report: {ex.Message}");
        }
    }

    public async Task<Result<PagedResultDto<TechnologyReportDto>>> GetReportsAsync(int pageNumber = 1, int pageSize = 10)
    {
        try
        {
            pageSize = Math.Min(pageSize, 100);

            var reports = await _reportRepository.GetReportsAsync(pageNumber, pageSize);
            var totalCount = await _reportRepository.GetReportsCountAsync();

            var dtos = new List<TechnologyReportDto>();
            foreach (var report in reports)
            {
                dtos.Add(await MapToDtoAsync(report));
            }

            var result = new PagedResultDto<TechnologyReportDto>
            {
                Items = dtos,
                TotalCount = totalCount,
       PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Result<PagedResultDto<TechnologyReportDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reports");
            return Result<PagedResultDto<TechnologyReportDto>>.Failure($"Error retrieving reports: {ex.Message}");
        }
    }

    public async Task<Result<TechnologyReportDto>> GetReportByIdAsync(Guid id)
    {
        try
        {
            var report = await _reportRepository.GetByIdWithResultsAsync(id);
            if (report == null)
                return Result<TechnologyReportDto>.Failure("Report not found");

            return Result<TechnologyReportDto>.Success(await MapToDtoAsync(report));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving report: {Id}", id);
            return Result<TechnologyReportDto>.Failure($"Error retrieving report: {ex.Message}");
        }
    }

    public async Task<Result<string>> GetReportPdfPathAsync(Guid id)
    {
        try
        {
            var report = await _reportRepository.GetByIdAsync(id);
            if (report == null)
                return Result<string>.Failure("Report not found");

            if (string.IsNullOrEmpty(report.PdfFilePath))
                return Result<string>.Failure("PDF not generated for this report");

            return Result<string>.Success(report.PdfFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving PDF path for report: {Id}", id);
            return Result<string>.Failure($"Error retrieving PDF: {ex.Message}");
        }
    }

    public async Task<Result<bool>> DeleteReportAsync(Guid id)
    {
        try
        {
            var report = await _reportRepository.GetByIdAsync(id);
            if (report == null)
                return Result<bool>.Failure("Report not found");

            // Delete PDF file if it exists
            if (!string.IsNullOrEmpty(report.PdfFilePath) && File.Exists(report.PdfFilePath))
            {
                try
                {
                    File.Delete(report.PdfFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete PDF file: {Path}", report.PdfFilePath);
                }
            }

            await _reportRepository.DeleteAsync(report);
            await _reportRepository.SaveChangesAsync();

            _logger.LogInformation("Report deleted: {Id}", id);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting report: {Id}", id);
            return Result<bool>.Failure($"Error deleting report: {ex.Message}");
        }
    }

    public async Task<Result<PagedResultDto<TechnologyReportDto>>> GetReportsByKeywordAsync(string keyword, int pageNumber = 1, int pageSize = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return Result<PagedResultDto<TechnologyReportDto>>.Failure("Keyword cannot be empty");

            pageSize = Math.Min(pageSize, 100);

            var reports = await _reportRepository.GetReportsForKeywordAsync(keyword, pageNumber, pageSize);
            var totalCount = await _context.TechnologyReports
                .Where(r => r.Keywords.Contains(keyword))
                .CountAsync();

            var dtos = new List<TechnologyReportDto>();
            foreach (var report in reports)
            {
                dtos.Add(await MapToDtoAsync(report));
            }

            var result = new PagedResultDto<TechnologyReportDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Result<PagedResultDto<TechnologyReportDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reports for keyword: {Keyword}", keyword);
            return Result<PagedResultDto<TechnologyReportDto>>.Failure($"Error retrieving reports: {ex.Message}");
        }
    }

    private async Task<TechnologyReportDto> MapToDtoAsync(Domain.Entities.TechnologyReport entity)
    {
        var keywords = new List<string>();
        if (!string.IsNullOrEmpty(entity.Keywords))
        {
            try
            {
                keywords = System.Text.Json.JsonSerializer.Deserialize<List<string>>(entity.Keywords) ?? new();
            }
            catch
            {
                // Log and ignore JSON parsing errors
            }
        }

        var results = new List<WebSearchResultDto>();
        if (entity.ReportResults != null && entity.ReportResults.Any())
        {
            // Load the search results if not already loaded
            if (!entity.ReportResults.Any(r => r.WebSearchResult != null))
            {
                var loaded = await _context.ReportResults
                    .Include(r => r.WebSearchResult)
                    .Where(r => r.ReportId == entity.Id)
                    .ToListAsync();

                results = loaded
                    .Select(r => new WebSearchResultDto
                    {
                        Id = r.WebSearchResult.Id,
                        Keyword = r.WebSearchResult.Keyword,
                        Title = r.WebSearchResult.Title,
                        Snippet = r.WebSearchResult.Snippet,
                        Url = r.WebSearchResult.Url,
                        PublishedDate = r.WebSearchResult.PublishedDate,
                        Source = r.WebSearchResult.Source,
                        RetrievedUtc = r.WebSearchResult.RetrievedUtc,
                        IsFromMonitoring = r.WebSearchResult.IsFromMonitoring
                    })
                    .ToList();
            }
            else
            {
                results = entity.ReportResults
                    .Select(r => new WebSearchResultDto
                    {
                        Id = r.WebSearchResult.Id,
                        Keyword = r.WebSearchResult.Keyword,
                        Title = r.WebSearchResult.Title,
                        Snippet = r.WebSearchResult.Snippet,
                        Url = r.WebSearchResult.Url,
                        PublishedDate = r.WebSearchResult.PublishedDate,
                        Source = r.WebSearchResult.Source,
                        RetrievedUtc = r.WebSearchResult.RetrievedUtc,
                        IsFromMonitoring = r.WebSearchResult.IsFromMonitoring
                    })
                    .ToList();
            }
        }

        return new TechnologyReportDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Keywords = keywords,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            GeneratedUtc = entity.GeneratedUtc,
            PdfUrl = !string.IsNullOrEmpty(entity.PdfFilePath) ? $"/api/technology-reports/{entity.Id}/pdf" : null,
            TotalResults = entity.TotalResults,
            Results = results,
            Summary = entity.Summary
        };
    }
}
