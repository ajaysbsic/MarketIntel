namespace Alfanar.MarketIntel.Domain.Entities;

public class TenderNotificationLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RuleId { get; set; }
    public Guid TenderNoticeId { get; set; }
    public Guid TenderVersionId { get; set; }
    public string Channel { get; set; } = "InApp";
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public string DeliveryStatus { get; set; } = "Pending";
    public string? ProviderMessageId { get; set; }
    public string DedupKey { get; set; } = string.Empty;
    /// <summary>Short headline shown in the notification inbox.</summary>
    public string? NotificationTitle { get; set; }
    /// <summary>One-line body rendered in toast/inbox.</summary>
    public string? NotificationBody { get; set; }
    /// <summary>Marks the notification as read by the user.</summary>
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }

    public TenderNotificationRule Rule { get; set; } = default!;
    public TenderNotice TenderNotice { get; set; } = default!;
    public TenderVersion TenderVersion { get; set; } = default!;
}
