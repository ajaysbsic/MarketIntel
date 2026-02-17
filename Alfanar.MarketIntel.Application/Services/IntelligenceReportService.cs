using Alfanar.MarketIntel.Application.Common;
using Alfanar.MarketIntel.Application.DTOs;
using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Domain.Entities;
using Alfanar.MarketIntel.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text;

namespace Alfanar.MarketIntel.Application.Services;

/// <summary>
/// Service for generating AI-powered intelligence reports from search results
/// </summary>
public class IntelligenceReportService : IIntelligenceReportService
{
    private readonly IIntelligenceReportRepository _reportRepository;
    private readonly IWebSearchResultRepository _searchRepository;
    private readonly IDocumentAnalyzer _documentAnalyzer;
    private readonly IFileStorageService _fileStorageService;
    private readonly PdfReportGenerator _pdfReportGenerator;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IntelligenceReportService> _logger;

    public IntelligenceReportService(
        IIntelligenceReportRepository reportRepository,
        IWebSearchResultRepository searchRepository,
        IDocumentAnalyzer documentAnalyzer,
        IFileStorageService fileStorageService,
        PdfReportGenerator pdfReportGenerator,
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<IntelligenceReportService> logger)
    {
        _reportRepository = reportRepository;
        _searchRepository = searchRepository;
        _documentAnalyzer = documentAnalyzer;
        _fileStorageService = fileStorageService;
        _pdfReportGenerator = pdfReportGenerator;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Result<IntelligenceReportDto>> GenerateReportAsync(GenerateIntelligenceReportRequestDto request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Keyword))
                return Result<IntelligenceReportDto>.Failure("Keyword is required");

            _logger.LogInformation("🚀 Generating HYBRID intelligence report for keyword: {Keyword}", request.Keyword);

            var startTime = DateTime.UtcNow;
            var fromDate = request.FromDate ?? DateTime.UtcNow.AddDays(-30);
            var toDate = request.ToDate ?? DateTime.UtcNow;

            // ═══════════════════════════════════════════════════════════════
            // STEP 1: ALWAYS query DATABASE for historical results
            // ═══════════════════════════════════════════════════════════════
            _logger.LogInformation("📊 [1/3] Querying DATABASE for keyword: {Keyword}", request.Keyword);
            
            var searchResults = await _searchRepository.GetResultsByKeywordAndDateRangeAsync(
                request.Keyword, fromDate, toDate);

            var dbCount = searchResults.Count;
            _logger.LogInformation("✅ Database query complete: {Count} articles found", dbCount);

            // ═══════════════════════════════════════════════════════════════
            // STEP 2: ALWAYS fetch LIVE from configured providers (NewsAPI, Google, etc.)
            // ═══════════════════════════════════════════════════════════════
            _logger.LogInformation("🌐 [2/3] Fetching LIVE from configured providers for keyword: {Keyword}", request.Keyword);
            
            var maxPerProvider = _configuration.GetValue<int>("IntelligenceReports:MaxArticlesPerProvider", 20);
            var liveResults = await FetchFromLiveProvidersAsync(request.Keyword, maxPerProvider);
            
            var liveCount = liveResults.Count;
            _logger.LogInformation("✅ Live fetch complete: {Count} fresh articles retrieved", liveCount);

            // ═══════════════════════════════════════════════════════════════
            // STEP 3: COMBINE & DEDUPLICATE (database + live sources)
            // ═══════════════════════════════════════════════════════════════
            _logger.LogInformation("🔗 [3/3] Combining results: {DbCount} database + {LiveCount} live = {Total} before deduplication", 
                dbCount, liveCount, dbCount + liveCount);

            // Combine both sources
            searchResults.AddRange(liveResults);

            // If STILL no results after both sources, generate synthetic report
            if (searchResults.Count == 0)
            {
                _logger.LogWarning("⚠️ No results from database OR live providers. Generating synthetic report.");
                return await GenerateSyntheticReportAsync(request, startTime);
            }

            _logger.LogInformation("📈 Combined article pool: {Count} total articles", searchResults.Count);

            // Deduplicate by URL
            var deduplicatedResults = searchResults
                .DistinctBy(r => r.Url)
                .OrderByDescending(r => r.PublishedDate ?? r.RetrievedUtc)
                .Take(request.MaxArticles)
                .ToList();

            _logger.LogInformation("Deduplicated to {Count} unique articles", deduplicatedResults.Count);

            // Strict relevance filtering to remove off-topic results
            var filteredResults = FilterRelevantArticles(deduplicatedResults, request.Keyword);
            _logger.LogInformation("🔎 Relevance filter retained {FilteredCount} of {TotalCount} articles", 
                filteredResults.Count, deduplicatedResults.Count);

            if (filteredResults.Count == 0)
            {
                _logger.LogWarning("⚠️ No relevant articles after filtering. Generating synthetic report.");
                return await GenerateSyntheticReportAsync(request, startTime);
            }

            // Create consolidated context from article titles and snippets
            var consolidatedText = BuildConsolidatedArticleText(filteredResults);

            // Call AI to generate intelligence report
            _logger.LogInformation("Calling AI to generate intelligence report...");
            var aiResult = await _documentAnalyzer.GenerateIntelligenceReportAsync(consolidatedText, request.Keyword);

            // Verify Gemini API call success
            if (aiResult.IsSuccess && aiResult.Data != null)
            {
                aiResult = Result<IntelligenceReportJsonDto>.Success(
                    EnsureReportSections(aiResult.Data, request.Keyword, filteredResults));

                _logger.LogInformation(
                    "✅ Gemini API Response Received | Keyword: {Keyword} | Model: {Model} | Tokens: {Tokens} | " +
                    "Sections: ExecutiveSummary={ExecLength} chars, MarketMovements={MarketLength} chars, " +
                    "Competitors={CompLength} chars, M&A={MaLength} chars, Policy={PolicyLength} chars, " +
                    "Tech={TechLength} chars, Funding={FundingLength} chars, Risks={RisksLength} chars",
                    request.Keyword,
                    _documentAnalyzer.GetType().Name,
                    aiResult.Data.TokensUsed ?? 0,
                    aiResult.Data.ExecutiveSummary?.Length ?? 0,
                    aiResult.Data.MarketMovements?.Length ?? 0,
                    aiResult.Data.CompetitorUpdates?.Length ?? 0,
                    aiResult.Data.MaSignals?.Length ?? 0,
                    aiResult.Data.PolicyAndRegulation?.Length ?? 0,
                    aiResult.Data.TechnologyDevelopments?.Length ?? 0,
                    aiResult.Data.InvestmentsAndFunding?.Length ?? 0,
                    aiResult.Data.RisksAndOpportunities?.Length ?? 0
                );
                
                // Log first 200 chars of executive summary to verify real content
                var preview = aiResult.Data.ExecutiveSummary?.Length > 0
                    ? aiResult.Data.ExecutiveSummary.Substring(0, Math.Min(200, aiResult.Data.ExecutiveSummary.Length))
                    : "(empty)";
                _logger.LogDebug("AI Report Preview: {Preview}...", preview);
            }
            else
            {
                _logger.LogError("❌ AI generation failed: {Error}", aiResult.Error);
            }

            if (!aiResult.IsSuccess)
                return Result<IntelligenceReportDto>.Failure($"AI generation failed: {aiResult.Error}");

            var reportData = aiResult.Data!;

            // Create report entity
            var report = new IntelligenceReport
            {
                Keyword = request.Keyword,
                GeneratedUtc = DateTime.UtcNow,
                Status = "Complete",
                ExecutiveSummary = reportData.ExecutiveSummary,
                MarketMovements = reportData.MarketMovements,
                CompetitorUpdates = reportData.CompetitorUpdates,
                MaSignals = reportData.MaSignals,
                PolicyAndRegulation = reportData.PolicyAndRegulation,
                TechnologyDevelopments = reportData.TechnologyDevelopments,
                InvestmentsAndFunding = reportData.InvestmentsAndFunding,
                RisksAndOpportunities = reportData.RisksAndOpportunities,
                RawArticleCount = searchResults.Count,
                DeduplicatedArticleCount = filteredResults.Count,
                AiModel = _documentAnalyzer.GetType().Name,
                TokensUsed = reportData.TokensUsed ?? 0,
                ProcessingTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds,
                FromDate = fromDate,
                ToDate = toDate
            };

            // Link source articles
            foreach (var result in filteredResults)
            {
                report.ReportResults.Add(new IntelligenceReportResult
                {
                    WebSearchResultId = result.Id
                });
            }

            // Save report to database
            await _reportRepository.AddAsync(report);
            await _reportRepository.SaveChangesAsync();

            // Generate PDF
            var pdfResult = await _pdfReportGenerator.GenerateIntelligenceReportPdfAsync(report, filteredResults);
            if (pdfResult.IsSuccess)
            {
                report.PdfFilePath = pdfResult.Data;
                await _reportRepository.UpdateAsync(report);
                await _reportRepository.SaveChangesAsync();
            }

            _logger.LogInformation("Intelligence report generated and saved: {ReportId}", report.Id);

            var sourceArticles = filteredResults
                .Select(result => new WebSearchResultDto
                {
                    Id = result.Id,
                    Keyword = result.Keyword,
                    Title = result.Title,
                    Snippet = result.Snippet,
                    Url = result.Url,
                    PublishedDate = result.PublishedDate,
                    Source = result.Source,
                    RetrievedUtc = result.RetrievedUtc,
                    IsFromMonitoring = result.IsFromMonitoring
                })
                .ToList();

            return Result<IntelligenceReportDto>.Success(await MapToDtoAsync(report, sourceArticles));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating intelligence report for keyword: {Keyword}", request.Keyword);
            return Result<IntelligenceReportDto>.Failure($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Fetches articles from all configured live search providers (extensible design)
    /// </summary>
    private async Task<List<Domain.Entities.WebSearchResult>> FetchFromLiveProvidersAsync(
        string keyword, 
        int maxArticlesPerProvider)
    {
        // Get configured live providers from appsettings (defaults to "newsapi")
        var providersConfig = _configuration.GetValue<string>("IntelligenceReports:LiveProviders", "newsapi");
        var enabledProviders = providersConfig.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                             .Select(p => p.Trim())
                                             .ToArray();

        _logger.LogInformation("🔍 Configured live providers: [{Providers}]", string.Join(", ", enabledProviders));

        var allLiveResults = new List<Domain.Entities.WebSearchResult>();

        // Get IWebSearchService lazily to avoid circular dependency
        var webSearchService = _serviceProvider.GetRequiredService<IWebSearchService>();

        // Fetch from each enabled provider
        foreach (var providerName in enabledProviders)
        {
            try
            {
                _logger.LogInformation("🌐 Fetching from {Provider} for keyword: {Keyword}", providerName, keyword);

                var searchRequest = new WebSearchRequestDto
                {
                    Keyword = keyword,
                    MaxResults = maxArticlesPerProvider,
                    SearchProvider = providerName
                };

                var liveSearchResult = await webSearchService.SearchAsync(searchRequest);

                if (liveSearchResult.IsSuccess && liveSearchResult.Data != null && liveSearchResult.Data.Any())
                {
                    var count = liveSearchResult.Data.Count;
                    _logger.LogInformation("✅ {Provider} returned {Count} articles", providerName, count);

                    // Convert DTOs to entities
                    var providerEntities = liveSearchResult.Data.Select(dto => new Domain.Entities.WebSearchResult
                    {
                        Id = dto.Id,
                        Keyword = dto.Keyword,
                        Title = dto.Title,
                        Snippet = dto.Snippet,
                        Url = dto.Url,
                        PublishedDate = dto.PublishedDate,
                        Source = dto.Source,
                        SearchProvider = dto.SearchProvider ?? providerName,
                        RetrievedUtc = dto.RetrievedUtc,
                        IsFromMonitoring = dto.IsFromMonitoring
                    }).ToList();

                    allLiveResults.AddRange(providerEntities);
                }
                else
                {
                    _logger.LogWarning("⚠️ {Provider} returned no results: {Error}", 
                        providerName, liveSearchResult.Error ?? "No data");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to fetch from {Provider}", providerName);
                // Continue with other providers
            }
        }

        _logger.LogInformation("📦 Total live results from all providers: {Count}", allLiveResults.Count);
        return allLiveResults;
    }

    private async Task<Result<IntelligenceReportDto>> GenerateSyntheticReportAsync(GenerateIntelligenceReportRequestDto request, DateTime startTime)
    {
        try
        {
            // Try to generate using AI first
            var syntheticPrompt = $@"Generate a professional intelligence report for the keyword: {request.Keyword}
            
No search results were available, so please provide a synthetic but informed analysis based on general market knowledge about this keyword/technology.

Include:
- Executive summary
- Market movements and trends
- Competitive landscape
- M&A and business signals
- Policy and regulation
- Technology developments
- Investments and funding
- Risks and opportunities

Keep the report professional and structured.";

            var aiResult = await _documentAnalyzer.GenerateIntelligenceReportAsync(syntheticPrompt, request.Keyword);

            // If AI fails, use a template-based approach
            if (!aiResult.IsSuccess)
            {
                _logger.LogWarning("AI generation failed ({Error}). Using template-based approach.", aiResult.Error);
                return GenerateTemplateReportAsync(request, startTime);
            }

            var reportData = aiResult.Data!;
            var fromDate = request.FromDate ?? DateTime.UtcNow.AddDays(-30);
            var toDate = request.ToDate ?? DateTime.UtcNow;

            // Create report entity with synthetic data
            var report = new IntelligenceReport
            {
                Keyword = request.Keyword,
                GeneratedUtc = DateTime.UtcNow,
                Status = "Synthetic",
                ExecutiveSummary = reportData.ExecutiveSummary,
                MarketMovements = reportData.MarketMovements,
                CompetitorUpdates = reportData.CompetitorUpdates,
                MaSignals = reportData.MaSignals,
                PolicyAndRegulation = reportData.PolicyAndRegulation,
                TechnologyDevelopments = reportData.TechnologyDevelopments,
                InvestmentsAndFunding = reportData.InvestmentsAndFunding,
                RisksAndOpportunities = reportData.RisksAndOpportunities,
                RawArticleCount = 0,
                DeduplicatedArticleCount = 0,
                AiModel = $"{_documentAnalyzer.GetType().Name} (Synthetic)",
                TokensUsed = reportData.TokensUsed ?? 0,
                ProcessingTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds,
                FromDate = fromDate,
                ToDate = toDate
            };

            // Save report to database
            await _reportRepository.AddAsync(report);
            await _reportRepository.SaveChangesAsync();

            // Generate PDF (synthetic version)
            var pdfResult = await _pdfReportGenerator.GenerateIntelligenceReportPdfAsync(report, new List<Domain.Entities.WebSearchResult>());
            if (pdfResult.IsSuccess)
            {
                report.PdfFilePath = pdfResult.Data;
                await _reportRepository.UpdateAsync(report);
                await _reportRepository.SaveChangesAsync();
            }

            _logger.LogInformation("Synthetic intelligence report generated and saved: {ReportId}", report.Id);

            return Result<IntelligenceReportDto>.Success(await MapToDtoAsync(report, new List<WebSearchResultDto>()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating synthetic intelligence report for keyword: {Keyword}", request.Keyword);
            return GenerateTemplateReportAsync(request, startTime);
        }
    }

    private Result<IntelligenceReportDto> GenerateTemplateReportAsync(GenerateIntelligenceReportRequestDto request, DateTime startTime)
    {
        try
        {
            var fromDate = request.FromDate ?? DateTime.UtcNow.AddDays(-30);
            var toDate = request.ToDate ?? DateTime.UtcNow;

            // Create a template-based report when AI is not available
            var report = new IntelligenceReport
            {
                Keyword = request.Keyword,
                GeneratedUtc = DateTime.UtcNow,
                Status = "Template",
                ExecutiveSummary = $@"Intelligence Report for: {request.Keyword}

This is a template-based intelligence report generated on {DateTime.UtcNow:MMMM dd, yyyy} for the keyword '{request.Keyword}'. The report provides market analysis and insights.",
                MarketMovements = $@"Market Movements for {request.Keyword}:

• The {request.Keyword} market continues to evolve with increasing adoption and innovation
• Key trends show growing integration in enterprise solutions
• Market dynamics indicate strong potential for growth
• Industry players are focusing on reliability and performance improvements",
                CompetitorUpdates = $@"Competitive Landscape:

• Major players in the {request.Keyword} space continue to invest in R&D
• New entrants are bringing innovative solutions to the market
• Strategic partnerships are shaping the industry direction
• Competition drives innovation and improved offerings",
                MaSignals = $@"M&A and Business Signals:

• Consolidation trends in the {request.Keyword} sector indicate industry maturation
• Strategic acquisitions focus on technology and talent acquisition
• Merger activity suggests market confidence and growth potential
• Recent funding rounds indicate investor interest in the space",
                PolicyAndRegulation = $@"Policy & Regulation:

• Regulatory developments affecting {request.Keyword} vary by region and sector
• Policy incentives and compliance requirements can accelerate adoption
• Standards and certifications influence market entry and procurement
• Monitoring policy updates remains important for strategic planning",
                TechnologyDevelopments = $@"Technology Developments:

• Advances in {request.Keyword} technologies continue to improve performance and reliability
• Innovation focuses on efficiency, scalability, and integration with adjacent systems
• R&D investment signals long-term maturity and capability improvements
• Technology roadmaps remain a key competitive differentiator",
                InvestmentsAndFunding = $@"Investments & Funding:

• Capital flows into {request.Keyword} reflect market confidence in growth potential
• Strategic investors seek technology differentiation and defensible IP
• Funding rounds typically target scaling, commercialization, and partnerships
• Investment activity can shift based on macroeconomic conditions",
                RisksAndOpportunities = $@"Risks and Opportunities:

Opportunities:
• Growing demand for {request.Keyword} solutions
• Digital transformation initiatives creating expansion opportunities
• Emerging markets present growth potential
• Technology advances enabling new use cases

Risks:
• Market competition may impact margins
• Regulatory changes could affect market dynamics
• Technology disruption could reshape the landscape
• Supply chain considerations require monitoring",
                RawArticleCount = 0,
                DeduplicatedArticleCount = 0,
                AiModel = "Template-Based (No AI)",
                TokensUsed = 0,
                ProcessingTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds,
                FromDate = fromDate,
                ToDate = toDate
            };

            // Save report to database
            _reportRepository.AddAsync(report).GetAwaiter().GetResult();
            _reportRepository.SaveChangesAsync().GetAwaiter().GetResult();

            _logger.LogInformation("Template-based intelligence report generated: {ReportId}", report.Id);

            return Result<IntelligenceReportDto>.Success(MapToDtoAsync(report, new List<WebSearchResultDto>()).GetAwaiter().GetResult());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating template report for keyword: {Keyword}", request.Keyword);
            return Result<IntelligenceReportDto>.Failure($"Error generating report: {ex.Message}");
        }
    }

    public async Task<Result<IntelligenceReportDto>> GetReportByIdAsync(Guid id)
    {
        try
        {
            var report = await _reportRepository.GetByIdWithResultsAsync(id);
            if (report == null)
                return Result<IntelligenceReportDto>.Failure("Report not found");

            var sourceArticles = report.ReportResults
                .Select(rr => new WebSearchResultDto
                {
                    Id = rr.WebSearchResult.Id,
                    Keyword = rr.WebSearchResult.Keyword,
                    Title = rr.WebSearchResult.Title,
                    Snippet = rr.WebSearchResult.Snippet,
                    Url = rr.WebSearchResult.Url,
                    PublishedDate = rr.WebSearchResult.PublishedDate,
                    Source = rr.WebSearchResult.Source,
                    RetrievedUtc = rr.WebSearchResult.RetrievedUtc,
                    IsFromMonitoring = rr.WebSearchResult.IsFromMonitoring
                })
                .ToList();

            return Result<IntelligenceReportDto>.Success(await MapToDtoAsync(report, sourceArticles));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving report: {ReportId}", id);
            return Result<IntelligenceReportDto>.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<Result<PagedResultDto<IntelligenceReportSummaryDto>>> GetReportsAsync(int pageNumber = 1, int pageSize = 10)
    {
        try
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Max(1, Math.Min(100, pageSize));

            var totalCount = await _reportRepository.GetReportsCountAsync();
            var reports = await _reportRepository.GetReportsAsync(pageNumber, pageSize);

            var items = reports.Select(r => new IntelligenceReportSummaryDto
            {
                Id = r.Id,
                Keyword = r.Keyword,
                GeneratedUtc = r.GeneratedUtc,
                Status = r.Status,
                DeduplicatedArticleCount = r.DeduplicatedArticleCount,
                ExecutiveSummary = r.ExecutiveSummary,
                PdfUrl = !string.IsNullOrEmpty(r.PdfFilePath) ? $"/api/intelligence-reports/{r.Id}/download-pdf" : null
            }).ToList();

            var result = new PagedResultDto<IntelligenceReportSummaryDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Result<PagedResultDto<IntelligenceReportSummaryDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reports");
            return Result<PagedResultDto<IntelligenceReportSummaryDto>>.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<Result<PagedResultDto<IntelligenceReportSummaryDto>>> GetReportsByKeywordAsync(string keyword, int pageNumber = 1, int pageSize = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return Result<PagedResultDto<IntelligenceReportSummaryDto>>.Failure("Keyword is required");

            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Max(1, Math.Min(100, pageSize));

            var totalCount = await _reportRepository.GetReportsCountByKeywordAsync(keyword);
            var reports = await _reportRepository.GetReportsByKeywordAsync(keyword, pageNumber, pageSize);

            var items = reports.Select(r => new IntelligenceReportSummaryDto
            {
                Id = r.Id,
                Keyword = r.Keyword,
                GeneratedUtc = r.GeneratedUtc,
                Status = r.Status,
                DeduplicatedArticleCount = r.DeduplicatedArticleCount,
                ExecutiveSummary = r.ExecutiveSummary,
                PdfUrl = !string.IsNullOrEmpty(r.PdfFilePath) ? $"/api/intelligence-reports/{r.Id}/download-pdf" : null
            }).ToList();

            var result = new PagedResultDto<IntelligenceReportSummaryDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Result<PagedResultDto<IntelligenceReportSummaryDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reports for keyword: {Keyword}", keyword);
            return Result<PagedResultDto<IntelligenceReportSummaryDto>>.Failure($"Error: {ex.Message}");
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
                return Result<string>.Failure("PDF not available for this report");

            return Result<string>.Success(report.PdfFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving PDF path for report: {ReportId}", id);
            return Result<string>.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<Result<byte[]>> DownloadReportPdfAsync(Guid id)
    {
        try
        {
            var report = await _reportRepository.GetByIdAsync(id);
            if (report == null)
                return Result<byte[]>.Failure("Report not found");

            if (string.IsNullOrEmpty(report.PdfFilePath))
                return Result<byte[]>.Failure("PDF not available for this report");

            // Download from file storage
            var pdfResult = await _fileStorageService.GetFileAsync(report.PdfFilePath);
            if (!pdfResult.IsSuccess)
                return Result<byte[]>.Failure(pdfResult.Error ?? "Failed to read PDF file");

            return Result<byte[]>.Success(pdfResult.Data ?? Array.Empty<byte>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading PDF for report: {ReportId}", id);
            return Result<byte[]>.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<Result<bool>> DeleteReportAsync(Guid id)
    {
        try
        {
            var report = await _reportRepository.GetByIdAsync(id);
            if (report == null)
                return Result<bool>.Failure("Report not found");

            // Delete PDF if exists
            if (!string.IsNullOrEmpty(report.PdfFilePath))
            {
                try
                {
                    await _fileStorageService.DeleteFileAsync(report.PdfFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete PDF file: {Path}", report.PdfFilePath);
                }
            }

            // Delete report from database
            await _reportRepository.DeleteAsync(report);
            await _reportRepository.SaveChangesAsync();

            _logger.LogInformation("Deleted intelligence report: {ReportId}", id);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting report: {ReportId}", id);
            return Result<bool>.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<Result<IntelligenceReportDto>> GetMostRecentReportAsync(string keyword)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return Result<IntelligenceReportDto>.Failure("Keyword is required");

            var report = await _reportRepository.GetMostRecentForKeywordAsync(keyword);
            if (report == null)
                return Result<IntelligenceReportDto>.Failure($"No reports found for keyword: {keyword}");

            var sourceArticles = report.ReportResults
                .Select(rr => new WebSearchResultDto
                {
                    Id = rr.WebSearchResult.Id,
                    Keyword = rr.WebSearchResult.Keyword,
                    Title = rr.WebSearchResult.Title,
                    Snippet = rr.WebSearchResult.Snippet,
                    Url = rr.WebSearchResult.Url,
                    PublishedDate = rr.WebSearchResult.PublishedDate,
                    Source = rr.WebSearchResult.Source,
                    RetrievedUtc = rr.WebSearchResult.RetrievedUtc,
                    IsFromMonitoring = rr.WebSearchResult.IsFromMonitoring
                })
                .ToList();

            return Result<IntelligenceReportDto>.Success(await MapToDtoAsync(report, sourceArticles));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving most recent report for keyword: {Keyword}", keyword);
            return Result<IntelligenceReportDto>.Failure($"Error: {ex.Message}");
        }
    }

    // Private helper methods

    private string BuildConsolidatedArticleText(List<WebSearchResult> articles)
    {
        var sb = new StringBuilder();

        foreach (var (article, index) in articles.Select((a, i) => (a, i)))
        {
            sb.AppendLine($"Article {index + 1}: {article.Title}");
            sb.AppendLine($"Source: {article.Source}");
            if (article.PublishedDate.HasValue)
                sb.AppendLine($"Date: {article.PublishedDate:yyyy-MM-dd}");
            sb.AppendLine($"URL: {article.Url}");
            sb.AppendLine();
            sb.AppendLine(article.Snippet);
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private List<WebSearchResult> FilterRelevantArticles(List<WebSearchResult> articles, string keyword)
    {
        if (articles.Count == 0)
            return articles;

        var keywordTokens = Tokenize(keyword);
        var normalizedKeyword = NormalizeText(keyword);
        var scored = new List<(WebSearchResult Article, double Score)>();

        foreach (var article in articles)
        {
            var title = article.Title ?? string.Empty;
            var snippet = article.Snippet ?? string.Empty;
            var combined = $"{title} {snippet}".Trim();

            if (string.IsNullOrWhiteSpace(combined))
                continue;

            var titleNorm = NormalizeText(title);
            var snippetNorm = NormalizeText(snippet);
            var combinedNorm = $"{titleNorm} {snippetNorm}".Trim();

            double score = 0;

            if (!string.IsNullOrWhiteSpace(normalizedKeyword))
            {
                if (titleNorm.Contains(normalizedKeyword))
                    score += 6;
                if (snippetNorm.Contains(normalizedKeyword))
                    score += 3;
                if (combinedNorm.Contains(normalizedKeyword))
                    score += 2;
            }

            foreach (var token in keywordTokens)
            {
                if (titleNorm.Contains(token))
                {
                    score += 2;
                }
                else if (snippetNorm.Contains(token))
                {
                    score += 1;
                }
            }

            if (keywordTokens.Count > 1 && keywordTokens.All(token => combinedNorm.Contains(token)))
                score += 3;

            if (IsSpammy(combinedNorm))
                score -= 6;

            if (title.Length < 10)
                score -= 1;

            scored.Add((article, score));
        }

        const double minScore = 6.0;

        return scored
            .Where(item => item.Score >= minScore)
            .OrderByDescending(item => item.Score)
            .Select(item => item.Article)
            .ToList();
    }

    private IntelligenceReportJsonDto EnsureReportSections(
        IntelligenceReportJsonDto report,
        string keyword,
        List<WebSearchResult> articles)
    {
        return new IntelligenceReportJsonDto
        {
            ExecutiveSummary = EnsureSection(report.ExecutiveSummary, "Executive Summary", keyword, articles),
            MarketMovements = EnsureSection(report.MarketMovements, "Market Movements", keyword, articles),
            CompetitorUpdates = EnsureSection(report.CompetitorUpdates, "Competitor Updates", keyword, articles),
            MaSignals = EnsureSection(report.MaSignals, "M&A Signals", keyword, articles),
            PolicyAndRegulation = EnsureSection(report.PolicyAndRegulation, "Policy & Regulation", keyword, articles),
            TechnologyDevelopments = EnsureSection(report.TechnologyDevelopments, "Technology Developments", keyword, articles),
            InvestmentsAndFunding = EnsureSection(report.InvestmentsAndFunding, "Investments & Funding", keyword, articles),
            RisksAndOpportunities = EnsureSection(report.RisksAndOpportunities, "Risks & Opportunities", keyword, articles),
            TokensUsed = report.TokensUsed
        };
    }

    private string EnsureSection(string? value, string sectionTitle, string keyword, List<WebSearchResult> articles)
    {
        if (!string.IsNullOrWhiteSpace(value))
            return value;

        return BuildFallbackSection(sectionTitle, keyword, articles);
    }

    private string BuildFallbackSection(string sectionTitle, string keyword, List<WebSearchResult> articles)
    {
        if (articles.Count == 0)
        {
            return $"No sufficiently relevant sources were found for {keyword} in the selected date range.";
        }

        var topArticles = articles
            .OrderByDescending(a => a.PublishedDate ?? a.RetrievedUtc)
            .Take(3)
            .ToList();

        var highlights = topArticles
            .Select(article =>
            {
                var snippet = string.IsNullOrWhiteSpace(article.Snippet) ? article.Title : article.Snippet;
                return TrimToSentence(snippet, 220);
            })
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToList();

        var summary = highlights.Count == 0
            ? $"Recent coverage of {keyword} provides limited detail. Consider broadening the date range or refining the keyword." 
            : string.Join(" ", highlights);

        return $"{sectionTitle} for {keyword}: {summary}";
    }

    private static string TrimToSentence(string text, int maxChars)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var clean = text.Trim();
        if (clean.Length <= maxChars)
            return clean;

        var truncated = clean.Substring(0, maxChars);
        var lastPeriod = truncated.LastIndexOf('.');
        if (lastPeriod > 50)
            return truncated.Substring(0, lastPeriod + 1);

        return truncated.TrimEnd() + "...";
    }

    private static List<string> Tokenize(string text)
    {
        var normalized = NormalizeText(text);
        var tokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        var stopWords = new HashSet<string> { "the", "and", "of", "for", "to", "in", "on", "with", "a", "an" };
        return tokens.Where(token => !stopWords.Contains(token)).Distinct().ToList();
    }

    private static string NormalizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var ch in text.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch))
            {
                sb.Append(ch);
            }
            else
            {
                sb.Append(' ');
            }
        }

        return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static bool IsSpammy(string text)
    {
        var spamMarkers = new[]
        {
            "buy now",
            "refurbished",
            "discount",
            "coupon",
            "deal",
            "job",
            "hiring",
            "apply now",
            "subscription",
            "promo",
            "laptop",
            "iphone",
            "android",
            "aud",
            "monthly",
            "click here"
        };

        return spamMarkers.Any(marker => text.Contains(marker));
    }

    private async Task<IntelligenceReportDto> MapToDtoAsync(IntelligenceReport report, List<WebSearchResultDto>? sourceArticles = null)
    {
        return new IntelligenceReportDto
        {
            Id = report.Id,
            Keyword = report.Keyword,
            GeneratedUtc = report.GeneratedUtc,
            Status = report.Status,
            ExecutiveSummary = report.ExecutiveSummary,
            MarketMovements = report.MarketMovements,
            CompetitorUpdates = report.CompetitorUpdates,
            MaSignals = report.MaSignals,
            PolicyAndRegulation = report.PolicyAndRegulation,
            TechnologyDevelopments = report.TechnologyDevelopments,
            InvestmentsAndFunding = report.InvestmentsAndFunding,
            RisksAndOpportunities = report.RisksAndOpportunities,
            RawArticleCount = report.RawArticleCount,
            DeduplicatedArticleCount = report.DeduplicatedArticleCount,
            AiModel = report.AiModel,
            TokensUsed = report.TokensUsed,
            ProcessingTimeMs = report.ProcessingTimeMs,
            PdfFilePath = report.PdfFilePath,
            PdfUrl = !string.IsNullOrEmpty(report.PdfFilePath) ? $"/api/intelligence-reports/{report.Id}/download-pdf" : null,
            ErrorMessage = report.ErrorMessage,
            FromDate = report.FromDate,
            ToDate = report.ToDate,
            SourceArticles = sourceArticles ?? new()
        };
    }
}
