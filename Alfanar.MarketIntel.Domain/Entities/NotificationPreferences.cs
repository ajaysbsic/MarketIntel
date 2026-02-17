namespace Alfanar.MarketIntel.Domain.Entities;

public class NotificationPreferences
{
    public string UserId { get; set; } = string.Empty;
    public bool EmailEnabled { get; set; } = true;
    public string? EmailAddress { get; set; }
    public bool NotifyOnCritical { get; set; } = true;
    public bool NotifyOnHigh { get; set; } = true;
    public bool NotifyOnMedium { get; set; } = false;
    public List<string> AlertTypesToNotify { get; set; } = new();
    public List<string> KeywordsToNotify { get; set; } = new();
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
