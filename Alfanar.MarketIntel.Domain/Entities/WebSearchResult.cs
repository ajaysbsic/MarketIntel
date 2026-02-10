namespace Alfanar.MarketIntel.Domain.Entities;

public class WebSearchResult
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? KeywordMonitorId { get; set; } // Nullable for ad-hoc searches

    public string Keyword { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Snippet { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public DateTime? PublishedDate { get; set; }

    public string Source { get; set; } = string.Empty;

    public string SearchProvider { get; set; } = "google"; // google, bing, serpapi

    public DateTime RetrievedUtc { get; set; } = DateTime.UtcNow;

    public bool IsFromMonitoring { get; set; }

    public string? Metadata { get; set; } // JSON for provider-specific data

    // Navigation
    public KeywordMonitor? KeywordMonitor { get; set; }
    public ICollection<ReportResult> ReportResults { get; set; } = new List<ReportResult>();
}
