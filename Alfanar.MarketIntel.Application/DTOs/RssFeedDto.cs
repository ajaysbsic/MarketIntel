namespace Alfanar.MarketIntel.Application.DTOs;

public class RssFeedDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Url { get; set; } = default!;
    public string Category { get; set; } = "General";
    public string Region { get; set; } = "Global";
    public bool IsActive { get; set; }
    public DateTime? LastFetchedUtc { get; set; }
    public string? LastETag { get; set; }
    public int? ArticleCount { get; set; }
}
