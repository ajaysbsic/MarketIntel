namespace Alfanar.MarketIntel.Domain.Entities;

/// <summary>
/// Represents AI-generated analysis and insights for a financial report
/// </summary>
public class ReportAnalysis
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Foreign key to FinancialReport (One-to-One)
    /// </summary>
    public Guid FinancialReportId { get; set; }
    
    /// <summary>
    /// Executive summary of the entire report
    /// </summary>
    public string ExecutiveSummary { get; set; } = default!;
    
    /// <summary>
    /// Key highlights (bullet points or JSON array)
    /// </summary>
    public string KeyHighlights { get; set; } = default!;
    
    /// <summary>
    /// Financial metrics extracted (JSON format)
    /// Example: { "revenue": 5.2B, "growth": 15%, "eps": 2.45 }
    /// </summary>
    public string? FinancialMetrics { get; set; }
    
    /// <summary>
    /// Strategic initiatives mentioned in the report
    /// </summary>
    public string? StrategicInitiatives { get; set; }
    
    /// <summary>
    /// Market trends and outlook
    /// </summary>
    public string? MarketOutlook { get; set; }
    
    /// <summary>
    /// Risk factors identified
    /// </summary>
    public string? RiskFactors { get; set; }
    
    /// <summary>
    /// Competitive positioning insights
    /// </summary>
    public string? CompetitivePosition { get; set; }
    
    /// <summary>
    /// Investment thesis or recommendations
    /// </summary>
    public string? InvestmentThesis { get; set; }
    
    /// <summary>
    /// Sentiment score (-1.0 to 1.0, negative to positive)
    /// </summary>
    public double? SentimentScore { get; set; }
    
    /// <summary>
    /// Overall sentiment (e.g., "Positive", "Neutral", "Negative")
    /// </summary>
    public string? SentimentLabel { get; set; }
    
    /// <summary>
    /// Confidence score for the analysis (0.0 to 1.0)
    /// </summary>
    public double? AnalysisConfidence { get; set; }
    
    /// <summary>
    /// AI model used for analysis (e.g., "gpt-4o", "gpt-4o-mini")
    /// </summary>
    public string? AiModel { get; set; }
    
    /// <summary>
    /// Tokens used for analysis (cost tracking)
    /// </summary>
    public int? TokensUsed { get; set; }
    
    /// <summary>
    /// Processing time in milliseconds
    /// </summary>
    public long? ProcessingTimeMs { get; set; }
    
    /// <summary>
    /// Tags or categories assigned by AI
    /// </summary>
    public string? Tags { get; set; }
    
    /// <summary>
    /// Related companies or entities mentioned
    /// </summary>
    public string? RelatedEntities { get; set; }
    
    /// <summary>
    /// When the analysis was generated
    /// </summary>
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the analysis was last updated
    /// </summary>
    public DateTime? UpdatedUtc { get; set; }
    
    // Navigation property
    public FinancialReport FinancialReport { get; set; } = default!;
}
