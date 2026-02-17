namespace Alfanar.MarketIntel.Domain.Entities;

public class TrendSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Keyword { get; set; } = string.Empty;

    public DateTime SnapshotDate { get; set; }

    public int MentionCount { get; set; }

    public int NewsCount { get; set; }

    public int WebSearchCount { get; set; }

    public double AverageSentiment { get; set; }

    public string? TopSources { get; set; }

    public string? CompetitorMentionCounts { get; set; }

    public int SignalStrength { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
