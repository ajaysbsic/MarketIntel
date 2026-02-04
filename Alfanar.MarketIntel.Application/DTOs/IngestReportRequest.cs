namespace Alfanar.MarketIntel.Application.DTOs;

/// <summary>
/// Request DTO for ingesting a new financial report
/// </summary>
public class IngestReportRequest
{
    public string CompanyName { get; set; } = default!;
    public string ReportType { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string SourceUrl { get; set; } = default!;
    public string? DownloadUrl { get; set; }
    public string? FiscalQuarter { get; set; }
    public int? FiscalYear { get; set; }
    public DateTime? PublishedDate { get; set; }
    public string? Region { get; set; }
    public string? Sector { get; set; }
    public string? FilePath { get; set; }
    public long? FileSizeBytes { get; set; }
    public int? PageCount { get; set; }
    public string? Language { get; set; }
    public string? ExtractedText { get; set; }
    public bool RequiredOcr { get; set; } = false;
    public Dictionary<string, object>? Metadata { get; set; }
    
    /// <summary>
    /// Base64-encoded PDF content (used when DownloadUrl is inaccessible from server)
    /// </summary>
    public string? PdfContentBase64 { get; set; }
}
