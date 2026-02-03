using Alfanar.MarketIntel.Application.Common;
using Alfanar.MarketIntel.Application.DTOs;
using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Application.Services;
using Alfanar.MarketIntel.Domain.Entities;
using Alfanar.MarketIntel.Infrastructure.Persistence;
using Alfanar.MarketIntel.Infrastructure.Repositories;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Alfanar.MarketIntel.Application.Services;

public class ReportService : IReportService
{
    private readonly IFinancialReportRepository _reportRepository;
    private readonly IFinancialMetricRepository _metricRepository;
    private readonly ISmartAlertRepository _alertRepository;
    private readonly IDocumentAnalyzer _documentAnalyzer;
    private readonly MetricExtractionService _metricExtraction;
    private readonly AlertRulesEngine _alertEngine;
    private readonly ILogger<ReportService> _logger;
    private readonly MarketIntelDbContext _context;
    private readonly IFileStorageService _fileStorage;
    private readonly IHttpClientFactory _httpClientFactory;

    public ReportService(
        IFinancialReportRepository reportRepository,
        IFinancialMetricRepository metricRepository,
        ISmartAlertRepository alertRepository,
        IDocumentAnalyzer documentAnalyzer,
        MetricExtractionService metricExtraction,
        AlertRulesEngine alertEngine,
        ILogger<ReportService> logger,
        MarketIntelDbContext context,
        IFileStorageService fileStorage,
        IHttpClientFactory httpClientFactory)
    {
        _reportRepository = reportRepository;
        _metricRepository = metricRepository;
        _alertRepository = alertRepository;
        _documentAnalyzer = documentAnalyzer;
        _metricExtraction = metricExtraction;
        _alertEngine = alertEngine;
        _logger = logger;
        _context = context;
        _fileStorage = fileStorage;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<Result<FinancialReportDto>> IngestReportAsync(IngestReportRequest request)
    {
        try
        {
            // Check for duplicates
            if (await _reportRepository.ExistsBySourceUrlAsync(request.SourceUrl))
            {
                _logger.LogWarning("Duplicate report URL attempted: {Url}", request.SourceUrl);
                return Result<FinancialReportDto>.Failure("Report with this source URL already exists");
            }

            var storedFileResult = await DownloadAndStoreReportAsync(request);
            if (!storedFileResult.IsSuccess || storedFileResult.Data == null)
            {
                _logger.LogWarning("Failed to download or store report from {Url}: {Error}", request.DownloadUrl, storedFileResult.Error);
                return Result<FinancialReportDto>.Failure(storedFileResult.Error ?? "Failed to store report file");
            }

            var storedFile = storedFileResult.Data;

            // Create report entity
            var report = new FinancialReport
            {
                Id = Guid.NewGuid(),
                CompanyName = request.CompanyName,
                ReportType = request.ReportType,
                Title = request.Title,
                SourceUrl = request.SourceUrl,
                DownloadUrl = request.DownloadUrl,
                FilePath = storedFile.Path,
                FileSizeBytes = storedFile.SizeBytes,
                FiscalQuarter = request.FiscalQuarter,
                FiscalYear = request.FiscalYear,
                PublishedDate = request.PublishedDate,
                Region = request.Region ?? "Global",
                Sector = request.Sector,
                ExtractedText = request.ExtractedText,
                PageCount = request.PageCount,
                Language = request.Language ?? "en",
                RequiredOcr = request.RequiredOcr,
                ProcessingStatus = "Ingested",
                IsProcessed = false,
                CreatedUtc = DateTime.UtcNow
            };

            // Serialize metadata if provided
            if (request.Metadata != null)
            {
                report.Metadata = JsonSerializer.Serialize(request.Metadata);
            }

            // Save report
            await _reportRepository.AddAsync(report);
            await _reportRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Successfully ingested report: {Title} for {Company}",
                report.Title,
                report.CompanyName);

            // Start async processing if text is available
            if (!string.IsNullOrWhiteSpace(report.ExtractedText))
            {
                _ = Task.Run(async () => await ProcessReportAsync(report.Id));
            }

            return Result<FinancialReportDto>.Success(MapToDto(report));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ingesting report: {Title}", request.Title);
            return Result<FinancialReportDto>.Failure($"Failed to ingest report: {ex.Message}");
        }
    }

    public async Task<Result<FinancialReportDto>> UpdateReportAsync(Guid id, IngestReportRequest request)
    {
        try
        {
            var report = await _reportRepository.GetByIdAsync(id, includeRelated: false);
            if (report == null)
                return Result<FinancialReportDto>.Failure("Report not found");

            // Update fields
            report.CompanyName = request.CompanyName;
            report.ReportType = request.ReportType;
            report.Title = request.Title;
            report.SourceUrl = request.SourceUrl;
            report.DownloadUrl = request.DownloadUrl ?? report.DownloadUrl;
            if (request.FileSizeBytes.HasValue)
            {
                report.FileSizeBytes = request.FileSizeBytes.Value;
            }
            report.FiscalQuarter = request.FiscalQuarter;
            report.FiscalYear = request.FiscalYear;
            report.PublishedDate = request.PublishedDate;
            report.Region = request.Region ?? "Global";
            report.Sector = request.Sector;
            report.ExtractedText = request.ExtractedText;
            report.PageCount = request.PageCount;
            report.Language = request.Language ?? "en";
            report.RequiredOcr = request.RequiredOcr;

            if (request.Metadata != null)
            {
                report.Metadata = JsonSerializer.Serialize(request.Metadata);
            }

            await _reportRepository.UpdateAsync(report);
            await _reportRepository.SaveChangesAsync();

            _logger.LogInformation("Updated report {Id}", id);

            return Result<FinancialReportDto>.Success(MapToDto(report));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating report {Id}", id);
            return Result<FinancialReportDto>.Failure($"Failed to update report: {ex.Message}");
        }
    }

    public async Task<Result> DeleteReportAsync(Guid id)
    {
        try
        {
            var report = await _reportRepository.GetByIdAsync(id, includeRelated: false);
            if (report == null)
                return Result.Failure("Report not found");

            if (!string.IsNullOrWhiteSpace(report.FilePath))
            {
                var deleteResult = await _fileStorage.DeleteFileAsync(report.FilePath);
                if (!deleteResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to delete stored file for report {Id}: {Error}", id, deleteResult.Error);
                }
            }

            await _reportRepository.DeleteAsync(report);
            await _reportRepository.SaveChangesAsync();

            _logger.LogInformation("Deleted report {Id}", id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting report {Id}", id);
            return Result.Failure($"Failed to delete report: {ex.Message}");
        }
    }

    public async Task<Result<FinancialReportDto>> GetByIdAsync(Guid id, bool includeRelated = true)
    {
        try
        {
            var report = await _reportRepository.GetByIdAsync(id, includeRelated);
            if (report == null)
                return Result<FinancialReportDto>.Failure("Report not found");

            return Result<FinancialReportDto>.Success(MapToDto(report, includeRelated));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving report {Id}", id);
            return Result<FinancialReportDto>.Failure($"Failed to retrieve report: {ex.Message}");
        }
    }

    public async Task<Result<PaginatedList<FinancialReportDto>>> GetFilteredReportsAsync(ReportFilterDto filter)
    {
        try
        {
            var reports = await _reportRepository.GetFilteredAsync(
                filter.CompanyName,
                filter.ReportType,
                filter.FiscalYear,
                filter.FiscalQuarter,
                filter.Sector,
                filter.Region,
                filter.ProcessingStatus,
                filter.IsProcessed,
                filter.FromDate,
                filter.ToDate,
                filter.PageNumber,
                filter.PageSize);

            var totalCount = await _reportRepository.GetFilteredCountAsync(
                filter.CompanyName,
                filter.ReportType,
                filter.FiscalYear,
                filter.FiscalQuarter,
                filter.Sector,
                filter.Region,
                filter.ProcessingStatus,
                filter.IsProcessed,
                filter.FromDate,
                filter.ToDate);

            var dtos = reports.Select(r => MapToDto(r, includeRelated: true)).ToList();
            var paginatedList = new PaginatedList<FinancialReportDto>(
                dtos,
                totalCount,
                filter.PageNumber,
                filter.PageSize);

            return Result<PaginatedList<FinancialReportDto>>.Success(paginatedList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving filtered reports");
            return Result<PaginatedList<FinancialReportDto>>.Failure($"Failed to retrieve reports: {ex.Message}");
        }
    }

    public async Task<Result<List<FinancialReportDto>>> GetRecentReportsAsync(int count = 10)
    {
        try
        {
            var reports = await _reportRepository.GetRecentAsync(count);
            var dtos = reports.Select(r => MapToDto(r, includeRelated: false)).ToList();
            return Result<List<FinancialReportDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent reports");
            return Result<List<FinancialReportDto>>.Failure($"Failed to retrieve recent reports: {ex.Message}");
        }
    }

    public async Task<Result<List<FinancialReportDto>>> GetByCompanyAsync(string companyName)
    {
        try
        {
            var reports = await _reportRepository.GetByCompanyAsync(companyName);
            var dtos = reports.Select(r => MapToDto(r, includeRelated: false)).ToList();
            return Result<List<FinancialReportDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reports for company {Company}", companyName);
            return Result<List<FinancialReportDto>>.Failure($"Failed to retrieve company reports: {ex.Message}");
        }
    }

    public async Task<Result<List<FinancialReportDto>>> GetPendingProcessingAsync(int maxCount = 50)
    {
        try
        {
            var reports = await _reportRepository.GetPendingProcessingAsync(maxCount);
            var dtos = reports.Select(r => MapToDto(r, includeRelated: false)).ToList();
            return Result<List<FinancialReportDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending reports");
            return Result<List<FinancialReportDto>>.Failure($"Failed to retrieve pending reports: {ex.Message}");
        }
    }

    public async Task<Result<ReportAnalysisDto>> GetAnalysisAsync(Guid reportId)
    {
        try
        {
            var report = await _reportRepository.GetByIdAsync(reportId, includeRelated: true);
            if (report == null)
                return Result<ReportAnalysisDto>.Failure("Report not found");

            if (report.Analysis == null)
                return Result<ReportAnalysisDto>.Failure("Analysis not available for this report");

            return Result<ReportAnalysisDto>.Success(MapAnalysisToDto(report.Analysis));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving analysis for report {Id}", reportId);
            return Result<ReportAnalysisDto>.Failure($"Failed to retrieve analysis: {ex.Message}");
        }
    }

    public async Task<Result<ReportAnalysisDto>> GenerateAnalysisAsync(Guid reportId)
    {
        try
        {
            // Reload fresh from DB to avoid concurrency issues
            var report = await _reportRepository.GetByIdAsync(reportId, includeRelated: true);
            if (report == null)
                return Result<ReportAnalysisDto>.Failure("Report not found");

            if (string.IsNullOrWhiteSpace(report.ExtractedText))
                return Result<ReportAnalysisDto>.Failure("No extracted text available for analysis");

            // Generate analysis using AI with retry logic
            ReportAnalysis? analysis = null;
            int retryCount = 0;
            const int maxRetries = 3;

            while (retryCount < maxRetries && analysis == null)
            {
                try
                {
                    var analysisResult = await _documentAnalyzer.AnalyzeDocumentAsync(
                        report.ExtractedText,
                        report.CompanyName,
                        report.ReportType);

                    if (!analysisResult.IsSuccess)
                    {
                        if (retryCount < maxRetries - 1 && analysisResult.Error?.Contains("ServiceUnavailable") == true)
                        {
                            _logger.LogWarning("API overloaded, retrying in 5 seconds... (attempt {Attempt}/{Max})", retryCount + 1, maxRetries);
                            await Task.Delay(5000);
                            retryCount++;
                            continue;
                        }
                        return Result<ReportAnalysisDto>.Failure(analysisResult.Error ?? "Analysis failed");
                    }

                    analysis = analysisResult.Data!;
                    analysis.FinancialReportId = reportId;
                }
                catch (Exception ex) when (retryCount < maxRetries - 1)
                {
                    _logger.LogWarning(ex, "Error during analysis, retrying... (attempt {Attempt}/{Max})", retryCount + 1, maxRetries);
                    await Task.Delay(5000);
                    retryCount++;
                }
            }

            if (analysis == null)
                return Result<ReportAnalysisDto>.Failure("Failed to generate analysis after multiple attempts");

            // Save analysis - use separate context and handle concurrency
            return await SaveAnalysisWithRetryAsync(reportId, analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating analysis for report {Id}", reportId);
            return Result<ReportAnalysisDto>.Failure($"Failed to generate analysis: {ex.Message}");
        }
    }

    private async Task<Result<ReportAnalysisDto>> SaveAnalysisWithRetryAsync(Guid reportId, ReportAnalysis analysis)
    {
        int retryCount = 0;
        const int maxRetries = 3;

        while (retryCount < maxRetries)
        {
            try
            {
                // Use raw SQL to avoid concurrency conflicts on free tier
                analysis.FinancialReportId = reportId;
                if (analysis.Id == Guid.Empty)
                    analysis.Id = Guid.NewGuid();
                
                // Insert or update using raw SQL - avoid concurrency tracking
                var now = DateTime.UtcNow;
                
                try
                {
                    // Try to update first (for existing analysis)
                    var updateRows = await _context.Database.ExecuteSqlInterpolatedAsync(
                        $@"UPDATE ReportAnalyses SET 
                            AiModel = {analysis.AiModel},
                            ExecutiveSummary = {analysis.ExecutiveSummary},
                            KeyHighlights = {analysis.KeyHighlights},
                            FinancialMetrics = {analysis.FinancialMetrics},
                            RiskFactors = {analysis.RiskFactors},
                            InvestmentThesis = {analysis.InvestmentThesis},
                            MarketOutlook = {analysis.MarketOutlook},
                            StrategicInitiatives = {analysis.StrategicInitiatives},
                            RelatedEntities = {analysis.RelatedEntities},
                            SentimentLabel = {analysis.SentimentLabel},
                            SentimentScore = {analysis.SentimentScore},
                            AnalysisConfidence = {analysis.AnalysisConfidence},
                            ProcessingTimeMs = {analysis.ProcessingTimeMs},
                            TokensUsed = {analysis.TokensUsed},
                            UpdatedUtc = {now},
                            CompetitivePosition = {analysis.CompetitivePosition},
                            Tags = {analysis.Tags}
                        WHERE FinancialReportId = {reportId}"
                    );
                    
                    // If no rows updated, insert new record
                    if (updateRows == 0)
                    {
                        await _context.Database.ExecuteSqlInterpolatedAsync(
                            $@"INSERT INTO ReportAnalyses 
                                (Id, AiModel, ExecutiveSummary, KeyHighlights, FinancialMetrics, RiskFactors, 
                                 InvestmentThesis, MarketOutlook, StrategicInitiatives, RelatedEntities, 
                                 SentimentLabel, SentimentScore, AnalysisConfidence, ProcessingTimeMs, TokensUsed, 
                                 CreatedUtc, UpdatedUtc, CompetitivePosition, Tags, FinancialReportId)
                            VALUES 
                                ({analysis.Id}, {analysis.AiModel}, {analysis.ExecutiveSummary}, {analysis.KeyHighlights}, 
                                 {analysis.FinancialMetrics}, {analysis.RiskFactors}, {analysis.InvestmentThesis}, 
                                 {analysis.MarketOutlook}, {analysis.StrategicInitiatives}, {analysis.RelatedEntities}, 
                                 {analysis.SentimentLabel}, {analysis.SentimentScore}, {analysis.AnalysisConfidence}, 
                                 {analysis.ProcessingTimeMs}, {analysis.TokensUsed}, {now}, {now}, 
                                 {analysis.CompetitivePosition}, {analysis.Tags}, {reportId})"
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in analysis insert/update for report {Id}", reportId);
                    throw;
                }
                
                // Update report status via raw SQL
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $@"UPDATE FinancialReports SET IsProcessed = 1, ProcessedUtc = {now}, ProcessingStatus = 'Complete' WHERE Id = {reportId}"
                );
                
                // Clear tracking to ensure clean state
                _context.ChangeTracker.Clear();
                var saved = true;

                if (saved)
                {
                    _logger.LogInformation("? Analysis saved for report {Id}", reportId);
                    return Result<ReportAnalysisDto>.Success(MapAnalysisToDto(analysis));
                }
                else
                {
                    _logger.LogWarning("?? SaveChanges returned false for report {Id}", reportId);
                    return Result<ReportAnalysisDto>.Failure("Failed to persist analysis to database");
                }
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
            {
                retryCount++;
                if (retryCount >= maxRetries)
                {
                    _logger.LogError(ex, "? Concurrency conflict persisted after {Retries} retries for report {Id}", maxRetries, reportId);
                    return Result<ReportAnalysisDto>.Failure("Failed to save analysis due to database conflict");
                }

                _logger.LogWarning("?? Concurrency conflict, retrying... (attempt {Attempt}/{Max})", retryCount, maxRetries);
                await Task.Delay(2000); // Increase delay
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Error saving analysis for report {Id}", reportId);
                return Result<ReportAnalysisDto>.Failure($"Failed to save analysis: {ex.Message}");
            }
        }

        return Result<ReportAnalysisDto>.Failure("Failed to save analysis after all retry attempts");
    }

    public async Task<Result<List<ReportSectionDto>>> GetSectionsAsync(Guid reportId)
    {
        try
        {
            var report = await _reportRepository.GetByIdAsync(reportId, includeRelated: true);
            if (report == null)
                return Result<List<ReportSectionDto>>.Failure("Report not found");

            var dtos = report.Sections
                .OrderBy(s => s.OrderIndex)
                .Select(MapSectionToDto)
                .ToList();

            return Result<List<ReportSectionDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sections for report {Id}", reportId);
            return Result<List<ReportSectionDto>>.Failure($"Failed to retrieve sections: {ex.Message}");
        }
    }

    public async Task<Result<List<string>>> GetDistinctCompaniesAsync()
    {
        try
        {
            var companies = await _reportRepository.GetDistinctCompaniesAsync();
            return Result<List<string>>.Success(companies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving companies");
            return Result<List<string>>.Failure($"Failed to retrieve companies: {ex.Message}");
        }
    }

    public async Task<Result<List<string>>> GetDistinctReportTypesAsync()
    {
        try
        {
            var types = await _reportRepository.GetDistinctReportTypesAsync();
            return Result<List<string>>.Success(types);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving report types");
            return Result<List<string>>.Failure($"Failed to retrieve report types: {ex.Message}");
        }
    }

    public async Task<Result<List<string>>> GetDistinctSectorsAsync()
    {
        try
        {
            var sectors = await _reportRepository.GetDistinctSectorsAsync();
            return Result<List<string>>.Success(sectors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sectors");
            return Result<List<string>>.Failure($"Failed to retrieve sectors: {ex.Message}");
        }
    }

    public async Task<Result<Dictionary<string, int>>> GetReportCountByCompanyAsync()
    {
        try
        {
            var counts = await _reportRepository.GetReportCountByCompanyAsync();
            return Result<Dictionary<string, int>>.Success(counts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving report counts by company");
            return Result<Dictionary<string, int>>.Failure($"Failed to retrieve report counts: {ex.Message}");
        }
    }

    public async Task<Result<Dictionary<string, int>>> GetReportCountByStatusAsync()
    {
        try
        {
            var counts = await _reportRepository.GetReportCountByStatusAsync();
            return Result<Dictionary<string, int>>.Success(counts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving report counts by status");
            return Result<Dictionary<string, int>>.Failure($"Failed to retrieve report counts: {ex.Message}");
        }
    }

    public async Task<Result<string>> GetFilePathAsync(Guid reportId)
    {
        try
        {
            var report = await _reportRepository.GetByIdAsync(reportId, includeRelated: false);
            if (report == null)
                return Result<string>.Failure("Report not found");

            if (string.IsNullOrWhiteSpace(report.FilePath))
                return Result<string>.Failure("File path not available for this report");

            return Result<string>.Success(report.FilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving file path for report {Id}", reportId);
            return Result<string>.Failure($"Failed to retrieve file path: {ex.Message}");
        }
    }

    public async Task<Result> UpdateProcessingStatusAsync(Guid id, string status, string? errorMessage = null)
    {
        try
        {
            var report = await _reportRepository.GetByIdAsync(id, includeRelated: false);
            if (report == null)
                return Result.Failure("Report not found");

            report.ProcessingStatus = status;
            report.ErrorMessage = errorMessage;
            report.UpdatedUtc = DateTime.UtcNow;

            if (status == "Complete")
            {
                report.IsProcessed = true;
                report.ProcessedUtc = DateTime.UtcNow;
            }

            await _reportRepository.UpdateAsync(report);
            await _reportRepository.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating processing status for report {Id}", id);
            return Result.Failure($"Failed to update status: {ex.Message}");
        }
    }

    // Private helper methods

    private async Task<Result<StoredFile>> DownloadAndStoreReportAsync(IngestReportRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DownloadUrl))
        {
            return Result<StoredFile>.Failure("Download URL is required to ingest reports");
        }

        try
        {
            var client = _httpClientFactory.CreateClient("report-ingestion-downloader");
            client.Timeout = TimeSpan.FromSeconds(60);

            using var response = await client.GetAsync(request.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                return Result<StoredFile>.Failure($"Failed to download report from {request.DownloadUrl}. Status: {(int)response.StatusCode} {response.ReasonPhrase}");
            }

            var fileName = EnsureValidExtension(ResolveFileName(request, response));

            await using var responseStream = await response.Content.ReadAsStreamAsync();
            await using var bufferedStream = new MemoryStream();
            await responseStream.CopyToAsync(bufferedStream);
            bufferedStream.Position = 0;

            var subfolder = BuildSubfolder(request);

            var saveResult = await _fileStorage.SaveFileAsync(bufferedStream, fileName, subfolder);
            if (!saveResult.IsSuccess || saveResult.Data == null)
            {
                return Result<StoredFile>.Failure(saveResult.Error ?? "Failed to save file to storage");
            }

            var sizeBytes = bufferedStream.Length > 0
                ? bufferedStream.Length
                : response.Content.Headers.ContentLength ?? 0;

            return Result<StoredFile>.Success(new StoredFile(saveResult.Data, sizeBytes));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading or storing report from {Url}", request.DownloadUrl);
            return Result<StoredFile>.Failure("Failed to download or store report file");
        }
    }

    private string ResolveFileName(IngestReportRequest request, HttpResponseMessage response)
    {
        var headerName = response.Content.Headers.ContentDisposition?.FileNameStar
                         ?? response.Content.Headers.ContentDisposition?.FileName;

        if (!string.IsNullOrWhiteSpace(headerName))
        {
            var trimmed = headerName.Trim('"');
            if (!string.IsNullOrWhiteSpace(trimmed))
                return trimmed;
        }

        if (!string.IsNullOrWhiteSpace(request.DownloadUrl)
            && Uri.TryCreate(request.DownloadUrl, UriKind.Absolute, out var uri))
        {
            var urlFileName = Path.GetFileName(uri.LocalPath);
            if (!string.IsNullOrWhiteSpace(urlFileName))
                return urlFileName;
        }

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            var sanitizedTitle = SanitizePathSegment(request.Title).Replace(' ', '_');
            return sanitizedTitle.Length > 150 ? sanitizedTitle[..150] + ".pdf" : sanitizedTitle + ".pdf";
        }

        return $"report_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
    }

    private string EnsureValidExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return string.IsNullOrWhiteSpace(extension) ? fileName + ".pdf" : fileName;
    }

    private string BuildSubfolder(IngestReportRequest request)
    {
        var company = SanitizePathSegment(request.CompanyName);
        var year = request.FiscalYear?.ToString() ?? DateTime.UtcNow.Year.ToString();
        return Path.Combine(company, year);
    }

    private string SanitizePathSegment(string segment)
    {
        var invalidChars = Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).ToArray();
        var sanitized = string.Join("_", segment.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return sanitized.Replace("/", "_").Replace("\\", "_");
    }

    private record StoredFile(string Path, long SizeBytes);

    private async Task ProcessReportAsync(Guid reportId)
    {
        try
        {
            _logger.LogInformation("Starting background processing for report {Id}", reportId);
            
            await UpdateProcessingStatusAsync(reportId, "Processing");
            
            var report = await _reportRepository.GetByIdAsync(reportId, includeRelated: false);
            if (report == null)
            {
                _logger.LogWarning("Report {Id} not found", reportId);
                return;
            }

            // Extract metrics first (fast, no API calls)
            List<FinancialMetric> metrics = new();
            if (!string.IsNullOrWhiteSpace(report.ExtractedText))
            {
                _logger.LogInformation("Extracting metrics from report {Id}", reportId);
                metrics = _metricExtraction.ExtractMetrics(report);
                _logger.LogInformation("Extracted {Count} metrics from report {Id}", metrics.Count, reportId);
                
                // Save metrics to database
                if (metrics.Any())
                {
                    await _metricRepository.AddRangeAsync(metrics);
                    await _metricRepository.SaveChangesAsync();
                    _logger.LogInformation("Saved {Count} metrics to database", metrics.Count);
                }
            }

            // Evaluate alert rules
            _logger.LogInformation("Evaluating alert rules for report {Id}", reportId);
            var alerts = _alertEngine.EvaluateRules(report, metrics);
            _logger.LogInformation("Generated {Count} alerts for report {Id}", alerts.Count, reportId);
            
            // Save alerts to database
            if (alerts.Any())
            {
                await _alertRepository.AddRangeAsync(alerts);
                await _alertRepository.SaveChangesAsync();
                _logger.LogInformation("Saved {Count} alerts to database", alerts.Count);
                
                // TODO: Send SignalR notifications for critical alerts
            }
            
            // Generate analysis (optional, requires OpenAI)
            var result = await GenerateAnalysisAsync(reportId);
            
            if (!result.IsSuccess)
            {
                await UpdateProcessingStatusAsync(reportId, "Failed", result.Error);
                _logger.LogError("Failed to process report {Id}: {Error}", reportId, result.Error);
            }
            else
            {
                _logger.LogInformation("Successfully processed report {Id}", reportId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in background processing for report {Id}", reportId);
            await UpdateProcessingStatusAsync(reportId, "Failed", ex.Message);
        }
    }

    private FinancialReportDto MapToDto(FinancialReport report, bool includeRelated = false)
    {
        var dto = new FinancialReportDto
        {
            Id = report.Id,
            CompanyName = report.CompanyName,
            ReportType = report.ReportType,
            Title = report.Title,
            SourceUrl = report.SourceUrl,
            DownloadUrl = report.DownloadUrl,
            FilePath = report.FilePath,
            FileSizeBytes = report.FileSizeBytes,
            FiscalQuarter = report.FiscalQuarter,
            FiscalYear = report.FiscalYear,
            PublishedDate = report.PublishedDate,
            Region = report.Region,
            Sector = report.Sector,
            PageCount = report.PageCount,
            Language = report.Language,
            IsProcessed = report.IsProcessed,
            RequiredOcr = report.RequiredOcr,
            ProcessingStatus = report.ProcessingStatus,
            ErrorMessage = report.ErrorMessage,
            CreatedUtc = report.CreatedUtc,
            UpdatedUtc = report.UpdatedUtc,
            ProcessedUtc = report.ProcessedUtc,
            RelatedArticlesCount = report.RelatedArticles?.Count ?? 0
        };

        if (includeRelated)
        {
            if (report.Analysis != null)
            {
                dto.Analysis = MapAnalysisToDto(report.Analysis);
            }

            if (report.Sections != null && report.Sections.Any())
            {
                dto.Sections = report.Sections
                    .OrderBy(s => s.OrderIndex)
                    .Select(MapSectionToDto)
                    .ToList();
            }
        }

        return dto;
    }

    private ReportAnalysisDto MapAnalysisToDto(ReportAnalysis analysis)
    {
        var dto = new ReportAnalysisDto
        {
            Id = analysis.Id,
            ExecutiveSummary = analysis.ExecutiveSummary,
            StrategicInitiatives = analysis.StrategicInitiatives,
            MarketOutlook = analysis.MarketOutlook,
            RiskFactors = analysis.RiskFactors,
            CompetitivePosition = analysis.CompetitivePosition,
            InvestmentThesis = analysis.InvestmentThesis,
            SentimentScore = analysis.SentimentScore,
            SentimentLabel = analysis.SentimentLabel,
            AnalysisConfidence = analysis.AnalysisConfidence,
            AiModel = analysis.AiModel,
            TokensUsed = analysis.TokensUsed,
            ProcessingTimeMs = analysis.ProcessingTimeMs,
            CreatedUtc = analysis.CreatedUtc
        };

        // Parse JSON fields
        if (!string.IsNullOrWhiteSpace(analysis.KeyHighlights))
        {
            try
            {
                dto.KeyHighlights = JsonSerializer.Deserialize<List<string>>(analysis.KeyHighlights) ?? new();
            }
            catch
            {
                dto.KeyHighlights = new List<string> { analysis.KeyHighlights };
            }
        }

        if (!string.IsNullOrWhiteSpace(analysis.FinancialMetrics))
        {
            try
            {
                dto.FinancialMetrics = JsonSerializer.Deserialize<Dictionary<string, object>>(analysis.FinancialMetrics);
            }
            catch { }
        }

        if (!string.IsNullOrWhiteSpace(analysis.Tags))
        {
            try
            {
                dto.Tags = JsonSerializer.Deserialize<List<string>>(analysis.Tags);
            }
            catch { }
        }

        if (!string.IsNullOrWhiteSpace(analysis.RelatedEntities))
        {
            try
            {
                dto.RelatedEntities = JsonSerializer.Deserialize<List<string>>(analysis.RelatedEntities);
            }
            catch { }
        }

        return dto;
    }

    private ReportSectionDto MapSectionToDto(ReportSection section)
    {
        var dto = new ReportSectionDto
        {
            Id = section.Id,
            Title = section.Title,
            SectionType = section.SectionType,
            Content = section.Content,
            PageNumbers = section.PageNumbers,
            OrderIndex = section.OrderIndex,
            Summary = section.Summary,
            ExtractionConfidence = section.ExtractionConfidence
        };

        if (!string.IsNullOrWhiteSpace(section.KeyDataPoints))
        {
            try
            {
                dto.KeyDataPoints = JsonSerializer.Deserialize<Dictionary<string, object>>(section.KeyDataPoints);
            }
            catch { }
        }

        return dto;
    }
}
