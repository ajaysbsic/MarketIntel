namespace Alfanar.MarketIntel.Domain.Entities;

public enum NotificationStatus
{
    Pending,
    Processing,
    Sent,
    Failed,
    Failed_MaxRetries
}

public class NotificationQueue
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AlertId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string NotificationType { get; set; } = "Email";
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public string Recipient { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
}
