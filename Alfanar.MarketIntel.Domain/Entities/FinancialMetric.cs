namespace Alfanar.MarketIntel.Domain.Entities;

/// <summary>
/// Extracted financial metrics from reports (revenue, margins, etc.)
/// </summary>
public class FinancialMetric
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Reference to the financial report
    /// </summary>
    public Guid FinancialReportId { get; set; }
    public FinancialReport FinancialReport { get; set; } = default!;
    
    /// <summary>
    /// Type of metric (Revenue, EBITDA, Margin, GuidanceRevenue, etc.)
    /// </summary>
    public string MetricType { get; set; } = default!;
    
    /// <summary>
    /// Numeric value of the metric
    /// </summary>
    public decimal? Value { get; set; }
    
    /// <summary>
    /// Unit (USD, EUR, Percent, etc.)
    /// </summary>
    public string? Unit { get; set; }
    
    /// <summary>
    /// Time period (Q1 2024, FY 2024, etc.)
    /// </summary>
    public string? Period { get; set; }
    
    /// <summary>
    /// Previous period value for comparison
    /// </summary>
    public decimal? PreviousValue { get; set; }
    
    /// <summary>
    /// Change from previous period (absolute)
    /// </summary>
    public decimal? Change { get; set; }
    
    /// <summary>
    /// Change from previous period (percentage)
    /// </summary>
    public decimal? ChangePercent { get; set; }
    
    /// <summary>
    /// Confidence score of extraction (0.0 to 1.0)
    /// </summary>
    public double ConfidenceScore { get; set; }
    
    /// <summary>
    /// How the metric was extracted (Regex, GPT, Manual)
    /// </summary>
    public string ExtractionMethod { get; set; } = default!;
    
    /// <summary>
    /// Raw text from which metric was extracted
    /// </summary>
    public string? SourceText { get; set; }
    
    /// <summary>
    /// When the metric was extracted
    /// </summary>
    public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;
}
