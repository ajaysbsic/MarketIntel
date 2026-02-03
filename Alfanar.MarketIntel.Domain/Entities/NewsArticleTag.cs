namespace Alfanar.MarketIntel.Domain.Entities;

// Join entity for many-to-many relationship
public class NewsArticleTag
{
    public Guid NewsArticleId { get; set; }
    public int TagId { get; set; }
    
    // Navigation properties
    public NewsArticle NewsArticle { get; set; } = default!;
    public Tag Tag { get; set; } = default!;
}
