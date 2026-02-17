namespace Alfanar.MarketIntel.Application.DTOs;

/// <summary>
/// Internal JSON structure from AI response for intelligence report generation
/// </summary>
public class IntelligenceReportJsonDto
{
    public string? ExecutiveSummary { get; set; }

    public string? MarketMovements { get; set; }

    public string? CompetitorUpdates { get; set; }

    public string? MaSignals { get; set; }

    public string? PolicyAndRegulation { get; set; }

    public string? TechnologyDevelopments { get; set; }

    public string? InvestmentsAndFunding { get; set; }

    public string? RisksAndOpportunities { get; set; }

    public int? TokensUsed { get; set; }
}

/// <summary>
/// Request DTO for generating an intelligence report
/// </summary>
public class GenerateIntelligenceReportRequestDto
{
    public string Keyword { get; set; } = string.Empty;

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public int MaxArticles { get; set; } = 20;
}

/// <summary>
/// Response DTO for an intelligence report section
/// </summary>
public class IntelligenceReportSectionDto
{
    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public List<string> KeyPoints { get; set; } = new();
}

/// <summary>
/// Full intelligence report response DTO
/// </summary>
public class IntelligenceReportDto
{
    public Guid Id { get; set; }

    public string Keyword { get; set; } = string.Empty;

    public DateTime GeneratedUtc { get; set; }

    public string Status { get; set; } = "Pending"; // Pending, Processing, Complete, Failed

    public string? ExecutiveSummary { get; set; }

    public string? MarketMovements { get; set; }

    public string? CompetitorUpdates { get; set; }

    public string? MaSignals { get; set; }

    public string? PolicyAndRegulation { get; set; }

    public string? TechnologyDevelopments { get; set; }

    public string? InvestmentsAndFunding { get; set; }

    public string? RisksAndOpportunities { get; set; }

    public int RawArticleCount { get; set; }

    public int DeduplicatedArticleCount { get; set; }

    public string AiModel { get; set; } = string.Empty;

    public int TokensUsed { get; set; }

    public long ProcessingTimeMs { get; set; }

    public string? PdfFilePath { get; set; }

    public string? PdfUrl { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public List<WebSearchResultDto> SourceArticles { get; set; } = new();
}

/// <summary>
/// Summary of multiple intelligence reports for a list view
/// </summary>
public class IntelligenceReportSummaryDto
{
    public Guid Id { get; set; }

    public string Keyword { get; set; } = string.Empty;

    public DateTime GeneratedUtc { get; set; }

    public string Status { get; set; } = string.Empty;

    public int DeduplicatedArticleCount { get; set; }

    public string? ExecutiveSummary { get; set; }

    public string? PdfUrl { get; set; }
}
