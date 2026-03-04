namespace Alfanar.MarketIntel.Domain.Entities;

public class TenderAuditRaw
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SourceId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string RawPayloadJson { get; set; } = string.Empty;
    public string PayloadHash { get; set; } = string.Empty;
    public DateTime RetrievedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RetentionUntil { get; set; }

    public TenderSource Source { get; set; } = default!;
}
