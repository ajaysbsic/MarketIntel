using Alfanar.MarketIntel.Application.Common;
using Alfanar.MarketIntel.Application.DTOs;
using Alfanar.MarketIntel.Domain.Entities;

namespace Alfanar.MarketIntel.Application.Interfaces;

/// <summary>
/// Service for AI-powered document analysis
/// </summary>
public interface IDocumentAnalyzer
{
    /// <summary>
    /// Analyze a financial document and generate insights
    /// </summary>
    Task<Result<ReportAnalysis>> AnalyzeDocumentAsync(
        string text,
        string companyName,
        string reportType);

    /// <summary>
    /// Generate executive summary from document text
    /// </summary>
    Task<Result<string>> GenerateSummaryAsync(string text, int maxWords = 200);

    /// <summary>
    /// Extract key highlights from document
    /// </summary>
    Task<Result<List<string>>> ExtractKeyHighlightsAsync(string text, int maxHighlights = 7);

    /// <summary>
    /// Extract financial metrics from document
    /// </summary>
    Task<Result<Dictionary<string, object>>> ExtractFinancialMetricsAsync(string text);

    /// <summary>
    /// Analyze sentiment of the document
    /// </summary>
    Task<Result<(double score, string label)>> AnalyzeSentimentAsync(string text);

    /// <summary>
    /// Check if the service is available
    /// </summary>
    bool IsAvailable();

    /// <summary>
    /// Generate a structured intelligence report from consolidated article text
    /// </summary>
    Task<Result<IntelligenceReportJsonDto>> GenerateIntelligenceReportAsync(string consolidatedArticleText, string keyword);

    /// <summary>
    /// Generate a curated insight for a cluster of related articles
    /// </summary>
    Task<Result<CuratedItemInsightDto>> GenerateCurationInsightAsync(string clusterText, string keyword);

    /// <summary>
    /// Extract competitor mentions and suggestions from article text
    /// </summary>
    Task<Result<CompetitorDetectionResultDto>> ExtractCompetitorMentionsAsync(string text, List<string> knownCompetitors);

    /// <summary>
    /// Confirm alert type based on article text
    /// </summary>
    Task<Result<AlertConfirmationDto>> ConfirmAlertAsync(string text, string alertType, string prompt);
}
