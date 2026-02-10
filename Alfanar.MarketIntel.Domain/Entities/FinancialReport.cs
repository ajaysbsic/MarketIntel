namespace Alfanar.MarketIntel.Domain.Entities;

/// <summary>
/// Represents a financial report document (PDF, earnings report, investor presentation, etc.)
/// </summary>
public class FinancialReport
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Company name (e.g., "Schneider Electric", "Tesla", "ABB")
    /// </summary>
    public string CompanyName { get; set; } = default!;
    
    /// <summary>
    /// Report type (e.g., "Quarterly Earnings", "Annual Report", "Investor Presentation")
    /// </summary>
    public string ReportType { get; set; } = default!;
    
    /// <summary>
    /// Report title
    /// </summary>
    public string Title { get; set; } = default!;
    
    /// <summary>
    /// Source URL where the report was found
    /// </summary>
    public string SourceUrl { get; set; } = default!;
    
    /// <summary>
    /// Direct download URL for the PDF
    /// </summary>
    public string? DownloadUrl { get; set; }
    
    /// <summary>
    /// Local file path where PDF is stored
    /// </summary>
    public string? FilePath { get; set; }
    
    /// <summary>
    /// File size in bytes
    /// </summary>
    public long? FileSizeBytes { get; set; }
    
    /// <summary>
    /// Fiscal quarter (e.g., "Q1", "Q2", "Q3", "Q4")
    /// </summary>
    public string? FiscalQuarter { get; set; }
    
    /// <summary>
    /// Fiscal year (e.g., 2024, 2025)
    /// </summary>
    public int? FiscalYear { get; set; }
    
    /// <summary>
    /// Report publication date
    /// </summary>
    public DateTime? PublishedDate { get; set; }
    
    /// <summary>
    /// Geographic region or market segment
    /// </summary>
    public string? Region { get; set; }
    
    /// <summary>
    /// Industry sector (e.g., "Energy Management", "EV Charging", "Battery Storage")
    /// </summary>
    public string? Sector { get; set; }
    
    /// <summary>
    /// Raw extracted text from PDF
    /// </summary>
    public string? ExtractedText { get; set; }
    
    /// <summary>
    /// Number of pages in the document
    /// </summary>
    public int? PageCount { get; set; }
    
    /// <summary>
    /// Language of the document
    /// </summary>
    public string Language { get; set; } = "en";
    
    /// <summary>
    /// Whether the document has been processed
    /// </summary>
    public bool IsProcessed { get; set; } = false;
    
    /// <summary>
    /// Whether OCR was required for text extraction
    /// </summary>
    public bool RequiredOcr { get; set; } = false;
    
    /// <summary>
    /// Processing status (e.g., "Downloaded", "Extracting", "Analyzing", "Complete", "Failed")
    /// </summary>
    public string ProcessingStatus { get; set; } = "Pending";
    
    /// <summary>
    /// Error message if processing failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Metadata as JSON (for extensibility)
    /// </summary>
    public string? Metadata { get; set; }
    
    /// <summary>
    /// When the report was discovered/ingested
    /// </summary>
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the report was last updated
    /// </summary>
    public DateTime? UpdatedUtc { get; set; }
    
    /// <summary>
    /// When the report processing was completed
    /// </summary>
    public DateTime? ProcessedUtc { get; set; }
    
    // Navigation properties
    
    /// <summary>
    /// Structured sections extracted from the report
    /// </summary>
    public ICollection<ReportSection> Sections { get; set; } = new List<ReportSection>();
    
    /// <summary>
    /// AI-generated analysis and insights
    /// </summary>
    public ReportAnalysis? Analysis { get; set; }
    
    /// <summary>
    /// Related news articles
    /// </summary>
    public ICollection<NewsArticle> RelatedArticles { get; set; } = new List<NewsArticle>();

    /// <summary>
    /// Tags associated with the report
    /// </summary>
    public ICollection<FinancialReportTag> FinancialReportTags { get; set; } = new List<FinancialReportTag>();
}
