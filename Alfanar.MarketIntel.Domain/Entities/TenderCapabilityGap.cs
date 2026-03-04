namespace Alfanar.MarketIntel.Domain.Entities;

public class TenderCapabilityGap
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenderVersionId { get; set; }
    public string Requirement { get; set; } = string.Empty;
    public string? InternalCapability { get; set; }
    public string GapLevel { get; set; } = "Unknown";
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public TenderVersion TenderVersion { get; set; } = default!;
}
