namespace Alfanar.MarketIntel.Domain.Entities;

/// <summary>
/// Join table linking IntelligenceReports to their source WebSearchResults
/// </summary>
public class IntelligenceReportResult
{
    public Guid IntelligenceReportId { get; set; }

    public Guid WebSearchResultId { get; set; }

    // Navigation properties
    public IntelligenceReport IntelligenceReport { get; set; } = null!;

    public WebSearchResult WebSearchResult { get; set; } = null!;
}
