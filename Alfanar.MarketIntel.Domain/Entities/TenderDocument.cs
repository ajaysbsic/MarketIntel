namespace Alfanar.MarketIntel.Domain.Entities;

public class TenderDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenderNoticeId { get; set; }
    public string DocumentUrl { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public string? FileType { get; set; }
    public string? FileHash { get; set; }
    public string? StoragePath { get; set; }
    public DateTime RetrievedAt { get; set; } = DateTime.UtcNow;

    public TenderNotice TenderNotice { get; set; } = default!;
}
