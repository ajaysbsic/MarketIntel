// NewsArticle.cs
namespace Alfanar.MarketIntel.Domain.Entities;

public class NewsArticle
{
    public Guid Id { get; set; }
    public string Source { get; set; } = default!;
    public string Url { get; set; } = default!;
    public string Title { get; set; } = default!;
    public DateTime PublishedUtc { get; set; }
    public string Region { get; set; } = "Global";
    public string Category { get; set; } = "Uncategorized"; // Funding, M&A, Policy, Project, MarketMetrics
    public string Summary { get; set; } = "";
    public string BodyText { get; set; } = "";
    public bool IsProcessed { get; set; } = false;
    public double? ClassificationConfidence { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedUtc { get; set; }
    
    // Foreign keys
    public Guid? RssFeedId { get; set; }
    public Guid? RelatedFinancialReportId { get; set; }
    
    // Navigation properties
    public RssFeed? RssFeed { get; set; }
    public FinancialReport? RelatedFinancialReport { get; set; }
    public ICollection<NewsArticleTag> NewsArticleTags { get; set; } = new List<NewsArticleTag>();
}