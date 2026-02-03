namespace Alfanar.MarketIntel.Domain.Entities;

public class RssFeed
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Url { get; set; } = default!;
    public string Category { get; set; } = "General";
    public string Region { get; set; } = "Global";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastFetchedUtc { get; set; }
    public string? LastETag { get; set; }
    public string? LastModified { get; set; }
    
    // Navigation property
    public ICollection<NewsArticle> Articles { get; set; } = new List<NewsArticle>();
}
