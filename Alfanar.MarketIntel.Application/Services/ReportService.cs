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
            _logger.LogInformation(" Report saved, DbContext state: {DbContextState}", _context.ChangeTracker.HasChanges() ? "HasChanges" : "NoChanges");

            // Extract and save AI analysis if provided in metadata
            if (request.Metadata != null)
            {
                _logger.LogInformation(" Metadata received with {KeyCount} keys: {Keys}", 
                    request.Metadata.Count, 
                    string.Join(", ", request.Metadata.Keys));
                
                if (request.Metadata.ContainsKey("analysis"))
                {
                    var analysisValue = request.Metadata["analysis"];
                    _logger.LogInformation(" 'analysis' key found! Type: {Type}", analysisValue?.GetType().Name ?? "null");
                }
            }
            
            if (request.Metadata != null && request.Metadata.TryGetValue("analysis", out var analysisObj))
            {
                _logger.LogInformation(" Found 'analysis' key in metadata, Type: {Type}", analysisObj?.GetType().Name);
                try
                {
                    // Handle both JsonElement and Dictionary types
                    Dictionary<string, object>? analysisData = null;
                    
                    if (analysisObj is JsonElement jsonElement)
                    {
                        // If it's a JsonElement, convert to string first then deserialize
                        var jsonString = jsonElement.GetRawText();
                        _logger.LogInformation(" JsonElement raw text (first 200 chars): {Json}", 
                            jsonString.Length > 200 ? jsonString.Substring(0, 200) + "..." : jsonString);
                        analysisData = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);
                        _logger.LogInformation(" Deserialized to Dictionary with {Count} keys", analysisData?.Count ?? 0);
                    }
                    else if (analysisObj is Dictionary<string, object> dict)
                    {
                        analysisData = dict;
                        _logger.LogInformation(" Analysis is already a Dictionary with {Count} keys", dict.Count);
                    }
                    else if (analysisObj != null)
                    {
                        // Try to serialize and deserialize as last resort
                        var jsonString = JsonSerializer.Serialize(analysisObj);
                        _logger.LogInformation(" Serializing unknown type to JSON (first 200 chars): {Json}",
                            jsonString.Length > 200 ? jsonString.Substring(0, 200) + "..." : jsonString);
                        analysisData = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);
                    }
                    
                    if (analysisData != null && analysisData.Count > 0)
                    {
                        _logger.LogInformation(" Creating ReportAnalysis from metadata with {FieldCount} fields: {Fields}", 
                            analysisData.Count,
                            string.Join(", ", analysisData.Keys));
                        
                        // Log all field values for debugging
                        foreach (var kvp in analysisData)
                        {
                            var valueType = kvp.Value?.GetType().Name ?? "null";
                            _logger.LogDebug("  {Key}: {Type}", kvp.Key, valueType);
                        }
                        
                        var execSummary = GetStringValue(analysisData, "executive_summary");
                        var sentiment = GetStringValue(analysisData, "sentiment_label") ?? GetStringValue(analysisData, "sentiment");
                        var mainRisks = GetStringValue(analysisData, "main_risks") ?? GetStringValue(analysisData, "risk_factors");
                        var sentimentScore = GetDoubleValue(analysisData, "sentiment_score");
                        
                        _logger.LogInformation(" Extracted values - Summary: {SummaryLen} chars, Sentiment: {Sentiment}, SentimentScore: {Score}, Risks: {RisksLen} chars",
                            execSummary?.Length ?? 0, 
                            sentiment ?? "null",
                            sentimentScore,
                            mainRisks?.Length ?? 0);
                        
                        // Extract key highlights - default to empty array if not provided
                        string keyHighlightsJson = "[]";
                        if (analysisData.TryGetValue("key_highlights", out var highlights))
                        {
                            if (highlights is JsonElement je)
                            {
                                keyHighlightsJson = je.GetRawText();
                            }
                            else
                            {
                                keyHighlightsJson = JsonSerializer.Serialize(highlights);
                            }
                            _logger.LogInformation("    key_highlights extracted: {Length} chars", keyHighlightsJson.Length);
                        }
                        else
                        {
                            _logger.LogInformation("    key_highlights not found, using empty array default");
                        }

                        var analysis = new ReportAnalysis
                        {
                            Id = Guid.NewGuid(),
                            FinancialReportId = report.Id,
                            ExecutiveSummary = execSummary ?? "",
                            KeyHighlights = keyHighlightsJson,  // FIXED: Always set, never null
                            StrategicInitiatives = GetStringValue(analysisData, "strategic_initiatives"),
                            MarketOutlook = GetStringValue(analysisData, "market_outlook"),
                            RiskFactors = mainRisks,
                            CompetitivePosition = GetStringValue(analysisData, "competitive_position"),
                            InvestmentThesis = GetStringValue(analysisData, "investment_thesis"),
                            SentimentScore = sentimentScore,
                            // Try 'sentiment_label' first (from Python watcher), then fall back to 'sentiment'
                            SentimentLabel = sentiment ?? "Neutral",
                            AiModel = GetStringValue(analysisData, "model") ?? "gemini-2.5-flash",
                            CreatedUtc = DateTime.UtcNow
                        };

                        _logger.LogInformation(" Adding ReportAnalysis to context...");
                        _logger.LogInformation("   Analysis ID: {AnalysisId}, ReportId: {ReportId}, Summary length: {SummaryLen}", 
                            analysis.Id, analysis.FinancialReportId, analysis.ExecutiveSummary?.Length ?? 0);
                        
                        // Check DbContext state before adding
                        _logger.LogInformation("   DbContext state BEFORE add: ChangeTracker.HasChanges={HasChanges}, ChangeTracker.Entries.Count={EntryCount}",
                            _context.ChangeTracker.HasChanges(), _context.ChangeTracker.Entries().Count());
                        
                        await _context.ReportAnalyses.AddAsync(analysis);
                        
                        _logger.LogInformation("    Added to context, entity state: {State}", 
                            _context.Entry(analysis).State);
                        _logger.LogInformation("   DbContext state AFTER add: ChangeTracker.HasChanges={HasChanges}, ChangeTracker.Entries.Count={EntryCount}",
                            _context.ChangeTracker.HasChanges(), _context.ChangeTracker.Entries().Count());
                        
                        _logger.LogInformation(" Saving changes to database...");
                        try
                        {
                            int saveResult = await _context.SaveChangesAsync();
                            _logger.LogInformation("    SaveChangesAsync returned: {Result} (number of entities saved)", saveResult);
                        }
                        catch (Exception saveEx)
                        {
                            _logger.LogError(saveEx, "    SaveChangesAsync threw exception: {ExceptionType}: {Message}", 
                                saveEx.GetType().Name, saveEx.Message);
                            throw; // Re-throw to let the outer catch handle it
                        }
                        
                        _logger.LogInformation(" Successfully saved AI analysis for report {Id} with sentiment {Sentiment}", 
                            report.Id, analysis.SentimentLabel);
                    }
                    else
                    {
                        _logger.LogWarning(" Analysis data was null or empty (Count: {Count})", analysisData?.Count ?? -1);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, " Failed to extract AI analysis from metadata for report {Id}. Exception type: {ExceptionType}, Message: {Message}", 
                        report.Id, ex.GetType().Name, ex.Message);
                    if (ex.InnerException != null)
                    {
                        _logger.LogError(ex.InnerException, "   Inner exception");
                    }
                }
            }
            else
            {
                _logger.LogDebug(" No 'analysis' key found in metadata for report {Title}", report.Title);
            }

            _logger.LogInformation(
                "Successfully ingested report: {Title} for {Company}",
                report.Title,
                report.CompanyName);

            // Start async processing if text is available
            if (!string.IsNullOrWhiteSpace(report.ExtractedText))
            {
                _ = Task.Run(async () => await ProcessReportAsync(report.Id));
            }

            await ExtractAnalysisFromSavedMetadataAsync(report.Id);

            return Result<FinancialReportDto>.Success(MapToDto(report, includeRelated: true));
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
            {
                _logger.LogWarning("Analysis missing for report {Id}. Attempting metadata extraction...", reportId);
                await ExtractAnalysisFromSavedMetadataAsync(reportId);
                report = await _reportRepository.GetByIdAsync(reportId, includeRelated: true);
            }

            if (report?.Analysis == null)
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

    public async Task<Result<object>> ExtractAnalysisFromMetadataAsync(int maxCount = 50)
    {
        try
        {
            _logger.LogInformation(" Batch extracting analysis from metadata for up to {MaxCount} reports", maxCount);

            // Get all reports that have analysis in metadata but no ReportAnalyses record
            var reports = await _context.FinancialReports
                .Where(r => !_context.ReportAnalyses.Any(ra => ra.FinancialReportId == r.Id))
                .Take(maxCount)
                .ToListAsync();

            _logger.LogInformation("Found {Count} reports without analysis records", reports.Count);

            int extractedCount = 0;
            var errors = new List<string>();

            foreach (var report in reports)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(report.Metadata))
                        continue;

                    if (!report.Metadata.Contains("\"analysis\""))
                        continue;

                    await ExtractAnalysisFromSavedMetadataAsync(report.Id);
                    extractedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error extracting analysis for report {Id}", report.Id);
                    errors.Add($"Report {report.Id}: {ex.Message}");
                }
            }

            _logger.LogInformation(" Batch extraction complete: {Extracted} extracted, {Errors} errors", extractedCount, errors.Count);

            return Result<object>.Success(new
            {
                message = "Batch extraction complete",
                totalProcessed = reports.Count,
                extracted = extractedCount,
                failed = reports.Count - extractedCount,
                errors = errors.Any() ? errors : null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, " Exception during batch extraction from metadata");
            return Result<object>.Failure($"Failed to extract analysis from metadata: {ex.Message}");
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
        try
        {
            MemoryStream? pdfStream = null;
            string fileName;
            
            // If PDF content is provided as base64, use it directly
            if (!string.IsNullOrWhiteSpace(request.PdfContentBase64))
            {
                _logger.LogInformation("Using provided PDF content (base64) instead of downloading");
                
                byte[] pdfBytes = Convert.FromBase64String(request.PdfContentBase64);
                pdfStream = new MemoryStream(pdfBytes);
                
                // Generate filename from title or URL
                if (!string.IsNullOrWhiteSpace(request.Title))
                {
                    var sanitizedTitle = SanitizePathSegment(request.Title).Replace(' ', '_');
                    fileName = sanitizedTitle.Length > 150 ? sanitizedTitle[..150] + ".pdf" : sanitizedTitle + ".pdf";
                }
                else if (!string.IsNullOrWhiteSpace(request.DownloadUrl) && Uri.TryCreate(request.DownloadUrl, UriKind.Absolute, out var uri))
                {
                    fileName = Path.GetFileName(uri.LocalPath);
                }
                else
                {
                    fileName = $"report_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
                }
                
                fileName = EnsureValidExtension(fileName);
            }
            // Otherwise, download from URL
            else if (!string.IsNullOrWhiteSpace(request.DownloadUrl))
            {
                var client = _httpClientFactory.CreateClient("report-ingestion-downloader");
                client.Timeout = TimeSpan.FromSeconds(60);

                using var response = await client.GetAsync(request.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode)
                {
                    return Result<StoredFile>.Failure($"Failed to download report from {request.DownloadUrl}. Status: {(int)response.StatusCode} {response.ReasonPhrase}");
                }

                fileName = EnsureValidExtension(ResolveFileName(request, response));

                await using var responseStream = await response.Content.ReadAsStreamAsync();
                pdfStream = new MemoryStream();
                await responseStream.CopyToAsync(pdfStream);
                pdfStream.Position = 0;
            }
            else
            {
                return Result<StoredFile>.Failure("Either DownloadUrl or PdfContentBase64 is required to ingest reports");
            }

            var subfolder = BuildSubfolder(request);

            var saveResult = await _fileStorage.SaveFileAsync(pdfStream, fileName, subfolder);
            if (!saveResult.IsSuccess || saveResult.Data == null)
            {
                pdfStream?.Dispose();
                return Result<StoredFile>.Failure(saveResult.Error ?? "Failed to save file to storage");
            }

            var sizeBytes = pdfStream.Length;
            pdfStream?.Dispose();

            return Result<StoredFile>.Success(new StoredFile(saveResult.Data, sizeBytes));
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Invalid base64 format for PDF content");
            return Result<StoredFile>.Failure("Invalid PDF content format");
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

    /// <summary>
    /// Helper method to safely extract string values from analysis metadata
    /// </summary>
    private string? GetStringValue(Dictionary<string, object> data, string key)
    {
        if (data == null || !data.TryGetValue(key, out var value))
        {
            _logger.LogDebug(" GetStringValue: key '{Key}' not found in data", key);
            return null;
        }

        var result = value switch
        {
            string s => s,
            JsonElement je => je.GetString(),
            null => null,
            _ => value.ToString()
        };
        
        _logger.LogDebug(" GetStringValue('{Key}'): {ValueType} => '{Result}'", 
            key, value?.GetType().Name ?? "null", result ?? "(null)");
        
        return result;
    }

    /// <summary>
    /// Helper method to safely extract double values from analysis metadata
    /// </summary>
    private double GetDoubleValue(Dictionary<string, object> data, string key)
    {
        if (data == null || !data.TryGetValue(key, out var value))
        {
            _logger.LogDebug(" GetDoubleValue: key '{Key}' not found in data", key);
            return 0.0;
        }

        var result = value switch
        {
            double d => d,
            int i => (double)i,
            JsonElement je => je.TryGetDouble(out var d) ? d : 0.0,
            string s => double.TryParse(s, out var parsed) ? parsed : 0.0,
            null => 0.0,
            _ => double.TryParse(value.ToString(), out var parsed) ? parsed : 0.0
        };
        
        _logger.LogDebug(" GetDoubleValue('{Key}'): {ValueType} => {Result}", 
            key, value?.GetType().Name ?? "null", result);
        
        return result;
    }

    private async Task ExtractAnalysisFromSavedMetadataAsync(Guid reportId)
    {
        _logger.LogInformation(" FALLBACK METHOD CALLED for report {ReportId}", reportId);
        try
        {
            _logger.LogInformation(" FALLBACK: Attempting to extract analysis from saved metadata for report {ReportId}", reportId);
            
            var report = await _context.FinancialReports.FirstOrDefaultAsync(r => r.Id == reportId);
            if (report == null)
            {
                _logger.LogWarning(" FALLBACK: Report {ReportId} not found in database", reportId);
                return;
            }

            var existingAnalysis = await _context.ReportAnalyses.FirstOrDefaultAsync(ra => ra.FinancialReportId == reportId);
            if (existingAnalysis != null)
            {
                _logger.LogInformation(" FALLBACK: Analysis already exists for report {ReportId}, skipping", reportId);
                return;
            }

            if (string.IsNullOrWhiteSpace(report.Metadata))
            {
                _logger.LogWarning(" FALLBACK: Report {ReportId} has no metadata", reportId);
                return;
            }

            using (JsonDocument doc = JsonDocument.Parse(report.Metadata))
            {
                if (!doc.RootElement.TryGetProperty("analysis", out var analysisElement))
                {
                    _logger.LogWarning(" FALLBACK: No 'analysis' property found in metadata for report {ReportId}", reportId);
                    return;
                }

                var analysisJson = analysisElement.GetRawText();
                var analysisData = JsonSerializer.Deserialize<Dictionary<string, object>>(analysisJson);

                if (analysisData == null)
                {
                    _logger.LogWarning(" FALLBACK: Failed to deserialize analysis for report {ReportId}", reportId);
                    return;
                }

                var keyHighlightsJson = GetJsonArrayString(analysisData, "key_highlights") ?? "[]";
                var riskFactorsJson = GetJsonArrayString(analysisData, "main_risks")
                    ?? GetJsonArrayString(analysisData, "risk_factors")
                    ?? GetStringValue(analysisData, "main_risks")
                    ?? GetStringValue(analysisData, "risk_factors")
                    ?? "";

                var analysis = new ReportAnalysis
                {
                    Id = Guid.NewGuid(),
                    FinancialReportId = reportId,
                    ExecutiveSummary = GetStringValue(analysisData, "executive_summary") ?? "",
                    SentimentLabel = GetStringValue(analysisData, "sentiment_label") ?? "Neutral",
                    SentimentScore = GetDoubleValue(analysisData, "sentiment_score"),
                    KeyHighlights = keyHighlightsJson,
                    RiskFactors = riskFactorsJson,
                    CreatedUtc = DateTime.UtcNow
                };
                _logger.LogInformation(" FALLBACK: Extracted analysis from metadata. Summary length: {SummaryLength}", 
                    analysis.ExecutiveSummary?.Length ?? 0);

                _logger.LogInformation("   Adding to context: Id={AnalysisId}, ReportId={ReportId}", 
                    analysis.Id, analysis.FinancialReportId);
                
                // Check DbContext state before adding
                _logger.LogInformation("   DbContext state BEFORE add: ChangeTracker.HasChanges={HasChanges}, ChangeTracker.Entries.Count={EntryCount}",
                    _context.ChangeTracker.HasChanges(), _context.ChangeTracker.Entries().Count());
                
                await _context.ReportAnalyses.AddAsync(analysis);
                
                _logger.LogInformation("    Added, entity state: {State}", 
                    _context.Entry(analysis).State);
                _logger.LogInformation("   DbContext state AFTER add: ChangeTracker.HasChanges={HasChanges}, ChangeTracker.Entries.Count={EntryCount}",
                    _context.ChangeTracker.HasChanges(), _context.ChangeTracker.Entries().Count());
                
                _logger.LogInformation("   Calling SaveChangesAsync...");
                try
                {
                    int saveResult = await _context.SaveChangesAsync();
                    _logger.LogInformation("    SaveChangesAsync returned: {Result} (number of entities saved)", saveResult);
                }
                catch (Exception saveEx)
                {
                    _logger.LogError(saveEx, "    SaveChangesAsync threw exception: {ExceptionType}: {Message}", 
                        saveEx.GetType().Name, saveEx.Message);
                    throw; // Re-throw to let the outer catch handle it
                }

                _logger.LogInformation(" FALLBACK: Successfully saved analysis to ReportAnalyses table for report {ReportId}", reportId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, " FALLBACK EXCEPTION: Error extracting analysis from saved metadata for report {ReportId}. Message: {Message}", reportId, ex.Message);
        }
        _logger.LogInformation(" FALLBACK METHOD COMPLETED for report {ReportId}", reportId);
    }

    private string? GetJsonArrayString(Dictionary<string, object> data, string key)
    {
        if (data == null || !data.TryGetValue(key, out var value) || value == null)
            return null;

        if (value is JsonElement je)
        {
            return je.ValueKind == JsonValueKind.Array ? je.GetRawText() : null;
        }

        // Handle already serialized arrays or lists
        try
        {
            return JsonSerializer.Serialize(value);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// DIAGNOSTIC: Test SaveChangesAsync with demo data
    /// </summary>
    public async Task<Result<object>> TestSaveAnalysisAsync()
    {
        _logger.LogInformation(" TEST: Starting SaveChangesAsync diagnostic test");
        
        try
        {
            // Create a demo report
            _logger.LogInformation(" TEST: Creating demo report");
            var demoReport = new FinancialReport
            {
                Id = Guid.NewGuid(),
                Title = "TEST REPORT - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                CompanyName = "TEST COMPANY",
                ReportType = "Test",
                FilePath = "test/path/demo.pdf",
                SourceUrl = "https://test.example.com/report",
                FileSizeBytes = 1024,
                PageCount = 1,
                PublishedDate = DateTime.UtcNow,
                FiscalYear = DateTime.Now.Year,
                FiscalQuarter = "Q1",
                Region = "Global",
                Sector = "Test",
                Language = "en",
                ExtractedText = "This is a test report for diagnostic purposes.",
                ProcessingStatus = "Completed",
                CreatedUtc = DateTime.UtcNow
            };

            _logger.LogInformation(" TEST: Adding report to context");
            await _context.FinancialReports.AddAsync(demoReport);
            
            _logger.LogInformation(" TEST: Saving report");
            await _context.SaveChangesAsync();
            _logger.LogInformation(" TEST: Report saved. ID: {ReportId}", demoReport.Id);

            // Create demo analysis
            _logger.LogInformation(" TEST: Creating demo analysis");
            var demoAnalysis = new ReportAnalysis
            {
                Id = Guid.NewGuid(),
                FinancialReportId = demoReport.Id,
                ExecutiveSummary = "This is a test summary for diagnostic purposes.",
                SentimentLabel = "Positive",
                SentimentScore = 0.85,
                KeyHighlights = "Test highlights",
                RiskFactors = "Test risks",
                CreatedUtc = DateTime.UtcNow
            };

            _logger.LogInformation(" TEST: DbContext state BEFORE adding analysis");
            _logger.LogInformation("  - HasChanges: {HasChanges}", _context.ChangeTracker.HasChanges());
            _logger.LogInformation("  - Entries count: {Count}", _context.ChangeTracker.Entries().Count());

            _logger.LogInformation(" TEST: Adding analysis to context");
            await _context.ReportAnalyses.AddAsync(demoAnalysis);

            _logger.LogInformation(" TEST: DbContext state AFTER adding analysis");
            _logger.LogInformation("  - HasChanges: {HasChanges}", _context.ChangeTracker.HasChanges());
            _logger.LogInformation("  - Entries count: {Count}", _context.ChangeTracker.Entries().Count());
            _logger.LogInformation("  - Analysis entry state: {State}", _context.Entry(demoAnalysis).State);

            _logger.LogInformation(" TEST: Calling SaveChangesAsync for analysis");
            int saveResult = await _context.SaveChangesAsync();
            _logger.LogInformation(" TEST: SaveChangesAsync returned: {Result}", saveResult);

            _logger.LogInformation(" TEST: SUCCESS! Analysis saved with ID: {AnalysisId}", demoAnalysis.Id);

            // Verify it was saved
            _logger.LogInformation(" TEST: Verifying analysis was saved");
            var savedAnalysis = await _context.ReportAnalyses.FirstOrDefaultAsync(a => a.Id == demoAnalysis.Id);
            
            if (savedAnalysis == null)
            {
                _logger.LogError(" TEST: FAILED! Analysis not found in database after save!");
                return Result<object>.Failure("Analysis was not persisted to database");
            }

            _logger.LogInformation(" TEST: VERIFIED! Analysis exists in database");
            return Result<object>.Success(new
            {
                message = "Test successful! Analysis was saved and verified.",
                reportId = demoReport.Id,
                analysisId = demoAnalysis.Id,
                savedInDb = true
            });
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, " TEST: DbUpdateException during save");
            _logger.LogError("  - Inner exception: {InnerMessage}", dbEx.InnerException?.Message);
            if (dbEx.InnerException?.InnerException != null)
            {
                _logger.LogError("  - Inner-inner exception: {InnerInnerMessage}", dbEx.InnerException.InnerException.Message);
            }
            return Result<object>.Failure($"Database update error: {dbEx.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, " TEST: Exception during diagnostic test");
            _logger.LogError("  - Exception type: {ExceptionType}", ex.GetType().Name);
            _logger.LogError("  - Message: {Message}", ex.Message);
            if (ex.InnerException != null)
            {
                _logger.LogError("  - Inner exception: {InnerMessage}", ex.InnerException.Message);
            }
            return Result<object>.Failure($"Test failed: {ex.Message}");
        }
    }
}

