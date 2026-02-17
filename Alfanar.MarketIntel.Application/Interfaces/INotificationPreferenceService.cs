using Alfanar.MarketIntel.Domain.Entities;

namespace Alfanar.MarketIntel.Application.Interfaces;

public interface INotificationPreferenceService
{
    Task<NotificationPreferences> GetUserPreferencesAsync(string userId);
    Task SetUserPreferencesAsync(string userId, NotificationPreferences preferences);
    Task<List<NotificationPreferences>> GetUsersInterestedInAlertAsync(string alertType);
}
