namespace Alfanar.MarketIntel.Application.DTOs;

public class NewsArticleDto
{
    public Guid Id { get; set; }
    public string Source { get; set; } = default!;
    public string Url { get; set; } = default!;
    public string Title { get; set; } = default!;
    public DateTime PublishedUtc { get; set; }
    public string Region { get; set; } = "Global";
    public string Category { get; set; } = "Uncategorized";
    public string Summary { get; set; } = "";
    public string BodyText { get; set; } = "";
    public List<string> Tags { get; set; } = new();
    public bool IsProcessed { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? ProcessedUtc { get; set; }
}
