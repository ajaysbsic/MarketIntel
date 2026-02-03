using Alfanar.MarketIntel.Application.Common;
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
}
