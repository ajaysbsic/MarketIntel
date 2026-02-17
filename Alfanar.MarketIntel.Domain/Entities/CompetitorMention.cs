namespace Alfanar.MarketIntel.Domain.Entities;

public class CompetitorMention
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CompetitorId { get; set; }

    public string SourceType { get; set; } = string.Empty; // News, WebSearch, Report

    public Guid SourceId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Snippet { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public double? SentimentScore { get; set; }

    public string? SentimentLabel { get; set; }

    public string MentionContext { get; set; } = string.Empty;

    public DateTime DetectedUtc { get; set; } = DateTime.UtcNow;

    public bool IsAutoDetected { get; set; } = true;

    public Competitor? Competitor { get; set; }
}
