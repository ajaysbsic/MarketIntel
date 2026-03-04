namespace Alfanar.MarketIntel.Domain.Entities;

public class TenderSource
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "API";
    public string BaseUrl { get; set; } = string.Empty;
    public string? AuthMode { get; set; }
    public int PollPriority { get; set; } = 100;
    public int PollIntervalMin { get; set; } = 60;
    public string? RateLimitPolicyJson { get; set; }
    public string? ConnectorConfigJson { get; set; }
    public bool IsCanary { get; set; } = false;
    public string RolloutStage { get; set; } = "General";
    public bool IsEnabled { get; set; } = true;
    public string? LegalNotes { get; set; }
    public string? Owner { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public ICollection<TenderNotice> Notices { get; set; } = new List<TenderNotice>();
    public ICollection<TenderIngestionRun> IngestionRuns { get; set; } = new List<TenderIngestionRun>();
    public ICollection<TenderAuditRaw> AuditRawRecords { get; set; } = new List<TenderAuditRaw>();
}
