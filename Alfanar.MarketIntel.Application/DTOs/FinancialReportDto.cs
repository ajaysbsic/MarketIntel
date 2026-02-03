namespace Alfanar.MarketIntel.Application.DTOs;

/// <summary>
/// Response DTO for financial report data
/// </summary>
public class FinancialReportDto
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = default!;
    public string ReportType { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string SourceUrl { get; set; } = default!;
    public string? DownloadUrl { get; set; }
    public string? FilePath { get; set; }
    public long? FileSizeBytes { get; set; }
    public string? FiscalQuarter { get; set; }
    public int? FiscalYear { get; set; }
    public DateTime? PublishedDate { get; set; }
    public string? Region { get; set; }
    public string? Sector { get; set; }
    public int? PageCount { get; set; }
    public string Language { get; set; } = "en";
    public bool IsProcessed { get; set; }
    public bool RequiredOcr { get; set; }
    public string ProcessingStatus { get; set; } = "Pending";
    public string? ErrorMessage { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? UpdatedUtc { get; set; }
    public DateTime? ProcessedUtc { get; set; }
    
    // Related data
    public ReportAnalysisDto? Analysis { get; set; }
    public List<ReportSectionDto>? Sections { get; set; }
    public int RelatedArticlesCount { get; set; }
}

/// <summary>
/// DTO for report analysis data
/// </summary>
public class ReportAnalysisDto
{
    public Guid Id { get; set; }
    public string ExecutiveSummary { get; set; } = default!;
    public List<string> KeyHighlights { get; set; } = new();
    public Dictionary<string, object>? FinancialMetrics { get; set; }
    public string? StrategicInitiatives { get; set; }
    public string? MarketOutlook { get; set; }
    public string? RiskFactors { get; set; }
    public string? CompetitivePosition { get; set; }
    public string? InvestmentThesis { get; set; }
    public double? SentimentScore { get; set; }
    public string? SentimentLabel { get; set; }
    public double? AnalysisConfidence { get; set; }
    public string? AiModel { get; set; }
    public int? TokensUsed { get; set; }
    public long? ProcessingTimeMs { get; set; }
    public List<string>? Tags { get; set; }
    public List<string>? RelatedEntities { get; set; }
    public DateTime CreatedUtc { get; set; }
}

/// <summary>
/// DTO for report section data
/// </summary>
public class ReportSectionDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public string SectionType { get; set; } = default!;
    public string Content { get; set; } = default!;
    public string? PageNumbers { get; set; }
    public int OrderIndex { get; set; }
    public string? Summary { get; set; }
    public Dictionary<string, object>? KeyDataPoints { get; set; }
    public double? ExtractionConfidence { get; set; }
}
