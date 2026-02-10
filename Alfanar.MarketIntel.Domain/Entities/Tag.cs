namespace Alfanar.MarketIntel.Domain.Entities;

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string NormalizedName { get; set; } = default!; // For case-insensitive queries
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public ICollection<NewsArticleTag> NewsArticleTags { get; set; } = new List<NewsArticleTag>();
    public ICollection<FinancialReportTag> FinancialReportTags { get; set; } = new List<FinancialReportTag>();
}
