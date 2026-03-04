namespace Alfanar.MarketIntel.Domain.Entities;

public class TenderIngestionRun
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SourceId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public string Status { get; set; } = "Running";
    public int ItemsFetched { get; set; }
    public int ItemsNew { get; set; }
    public int ItemsUpdated { get; set; }
    public string? Errors { get; set; }
    public int RetryCount { get; set; }
    public string? WorkerId { get; set; }

    public TenderSource Source { get; set; } = default!;
}
