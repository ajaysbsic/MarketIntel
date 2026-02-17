namespace Alfanar.MarketIntel.Domain.Entities;

public class Competitor
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string Industry { get; set; } = string.Empty;

    public string Region { get; set; } = string.Empty;

    public string? Keywords { get; set; }

    public string? Website { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsAutoDetected { get; set; } = false;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public string? CreatedBy { get; set; }

    public string? Notes { get; set; }

    public ICollection<CompetitorMention> Mentions { get; set; } = new List<CompetitorMention>();
}
