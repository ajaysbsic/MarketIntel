namespace Alfanar.MarketIntel.Domain.Entities;

public class TenderVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenderNoticeId { get; set; }
    public int VersionNo { get; set; }
    public string RawHash { get; set; } = string.Empty;
    public string NormalizedHash { get; set; } = string.Empty;
    public string ChangeType { get; set; } = "Update";
    public string? ChangedFieldsJson { get; set; }
    public string? SnapshotJson { get; set; }
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    public TenderNotice TenderNotice { get; set; } = default!;
    public ICollection<TenderNotificationLog> NotificationLogs { get; set; } = new List<TenderNotificationLog>();
}
