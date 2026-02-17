namespace Alfanar.MarketIntel.Domain.Entities;

/// <summary>
/// Represents a structured AI-generated intelligence report for a keyword with 5 key sections
/// </summary>
public class IntelligenceReport
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Keyword { get; set; } = string.Empty;

    public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Status: Pending, Processing, Complete, Failed
    /// </summary>
    public string Status { get; set; } = "Pending"; // Pending, Processing, Complete, Failed

    /// <summary>
    /// Executive summary section of the report
    /// </summary>
    public string? ExecutiveSummary { get; set; }

    /// <summary>
    /// Market movements section - key market changes and trends
    /// </summary>
    public string? MarketMovements { get; set; }

    /// <summary>
    /// Competitor updates section - news about competitors
    /// </summary>
    public string? CompetitorUpdates { get; set; }

    /// <summary>
    /// M&A signals section - merger and acquisition signals
    /// </summary>
    public string? MaSignals { get; set; }

    /// <summary>
    /// Policy and regulation section - policy changes and regulatory impacts
    /// </summary>
    public string? PolicyAndRegulation { get; set; }

    /// <summary>
    /// Technology developments section - innovations and technical progress
    /// </summary>
    public string? TechnologyDevelopments { get; set; }

    /// <summary>
    /// Investments and funding section - capital flows and funding activity
    /// </summary>
    public string? InvestmentsAndFunding { get; set; }

    /// <summary>
    /// Risks and opportunities section
    /// </summary>
    public string? RisksAndOpportunities { get; set; }

    /// <summary>
    /// Count of unique articles analyzed
    /// </summary>
    public int RawArticleCount { get; set; }

    /// <summary>
    /// Count of articles after deduplication
    /// </summary>
    public int DeduplicatedArticleCount { get; set; }

    /// <summary>
    /// Which AI model was used (e.g., "gemini-2.5-flash", "gpt-4o-mini")
    /// </summary>
    public string AiModel { get; set; } = string.Empty;

    /// <summary>
    /// Tokens used in the AI call
    /// </summary>
    public int TokensUsed { get; set; }

    /// <summary>
    /// Time to generate report in milliseconds
    /// </summary>
    public long ProcessingTimeMs { get; set; }

    /// <summary>
    /// File path to the generated PDF (if applicable)
    /// </summary>
    public string? PdfFilePath { get; set; }

    /// <summary>
    /// Error message if processing failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Date range used for article search (start)
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Date range used for article search (end)
    /// </summary>
    public DateTime? ToDate { get; set; }

    // Navigation properties
    public ICollection<IntelligenceReportResult> ReportResults { get; set; } = new List<IntelligenceReportResult>();
}
