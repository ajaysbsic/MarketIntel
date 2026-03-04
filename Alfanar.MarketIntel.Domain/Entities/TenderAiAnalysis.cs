namespace Alfanar.MarketIntel.Domain.Entities;

public class TenderAiAnalysis
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenderVersionId { get; set; }
    public string? ExtractedRequirementsJson { get; set; }
    public decimal? Confidence { get; set; }
    public string? ModelName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public TenderVersion TenderVersion { get; set; } = default!;
}
