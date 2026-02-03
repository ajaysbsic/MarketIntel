namespace Alfanar.MarketIntel.Domain.Entities;

/// <summary>
/// Represents a structured section of a financial report
/// (e.g., Executive Summary, Financial Highlights, Market Outlook)
/// </summary>
public class ReportSection
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Foreign key to FinancialReport
    /// </summary>
    public Guid FinancialReportId { get; set; }
    
    /// <summary>
    /// Section title (e.g., "Executive Summary", "Revenue Analysis")
    /// </summary>
    public string Title { get; set; } = default!;
    
    /// <summary>
    /// Section type for categorization (e.g., "Summary", "Financial", "Outlook", "Risk")
    /// </summary>
    public string SectionType { get; set; } = default!;
    
    /// <summary>
    /// Raw text content of the section
    /// </summary>
    public string Content { get; set; } = default!;
    
    /// <summary>
    /// Page number(s) where section appears
    /// </summary>
    public string? PageNumbers { get; set; }
    
    /// <summary>
    /// Section order/position in the document
    /// </summary>
    public int OrderIndex { get; set; }
    
    /// <summary>
    /// AI-generated summary of the section
    /// </summary>
    public string? Summary { get; set; }
    
    /// <summary>
    /// Key data points extracted as JSON (e.g., financial metrics, percentages)
    /// </summary>
    public string? KeyDataPoints { get; set; }
    
    /// <summary>
    /// Confidence score for extraction accuracy (0.0 to 1.0)
    /// </summary>
    public double? ExtractionConfidence { get; set; }
    
    /// <summary>
    /// When the section was created
    /// </summary>
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public FinancialReport FinancialReport { get; set; } = default!;
}
