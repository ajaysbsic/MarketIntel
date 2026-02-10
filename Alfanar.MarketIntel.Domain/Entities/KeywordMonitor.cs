namespace Alfanar.MarketIntel.Domain.Entities;

public class KeywordMonitor
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Keyword { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public int CheckIntervalMinutes { get; set; } = 60;

    public DateTime? LastCheckedUtc { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public string? CreatedBy { get; set; }

    public string? Tags { get; set; } // JSON array for categorization

    public int MaxResultsPerCheck { get; set; } = 10;

    // Navigation
    public ICollection<WebSearchResult> WebSearchResults { get; set; } = new List<WebSearchResult>();
}
