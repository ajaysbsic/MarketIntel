namespace Alfanar.MarketIntel.Domain.Entities;

public class ReportResult
{
    public Guid ReportId { get; set; }

    public Guid WebSearchResultId { get; set; }

    // Navigation
    public TechnologyReport Report { get; set; } = null!;
    public WebSearchResult WebSearchResult { get; set; } = null!;
}
