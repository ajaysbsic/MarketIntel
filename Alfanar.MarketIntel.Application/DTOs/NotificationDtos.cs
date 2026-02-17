using Alfanar.MarketIntel.Domain.Entities;

namespace Alfanar.MarketIntel.Application.DTOs;

public class NotificationPreferencesDto
{
    public bool EmailEnabled { get; set; } = true;
    public string? EmailAddress { get; set; }
    public bool NotifyOnCritical { get; set; } = true;
    public bool NotifyOnHigh { get; set; } = true;
    public bool NotifyOnMedium { get; set; } = false;
    public List<string> AlertTypesToNotify { get; set; } = new();
    public List<string> KeywordsToNotify { get; set; } = new();

    public static NotificationPreferencesDto FromEntity(NotificationPreferences preferences)
    {
        return new NotificationPreferencesDto
        {
            EmailEnabled = preferences.EmailEnabled,
            EmailAddress = preferences.EmailAddress,
            NotifyOnCritical = preferences.NotifyOnCritical,
            NotifyOnHigh = preferences.NotifyOnHigh,
            NotifyOnMedium = preferences.NotifyOnMedium,
            AlertTypesToNotify = preferences.AlertTypesToNotify,
            KeywordsToNotify = preferences.KeywordsToNotify
        };
    }

    public NotificationPreferences ToEntity(string userId)
    {
        return new NotificationPreferences
        {
            UserId = userId,
            EmailEnabled = EmailEnabled,
            EmailAddress = EmailAddress,
            NotifyOnCritical = NotifyOnCritical,
            NotifyOnHigh = NotifyOnHigh,
            NotifyOnMedium = NotifyOnMedium,
            AlertTypesToNotify = AlertTypesToNotify ?? new List<string>(),
            KeywordsToNotify = KeywordsToNotify ?? new List<string>(),
            UpdatedUtc = DateTime.UtcNow
        };
    }
}
