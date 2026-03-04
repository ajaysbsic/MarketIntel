namespace Alfanar.MarketIntel.Domain.Entities;

public class TenderScore
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenderVersionId { get; set; }
    public decimal? WinProbability { get; set; }
    public decimal? RiskScore { get; set; }
    public string? ComponentsJson { get; set; }
    public string? ScoringModel { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public TenderVersion TenderVersion { get; set; } = default!;
}
