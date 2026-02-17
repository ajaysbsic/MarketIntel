using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Domain.Entities;
using Alfanar.MarketIntel.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace Alfanar.MarketIntel.Application.Services;

public class NotificationPreferenceService : INotificationPreferenceService
{
    private readonly INotificationPreferencesRepository _repository;
    private readonly ILogger<NotificationPreferenceService> _logger;

    public NotificationPreferenceService(
        INotificationPreferencesRepository repository,
        ILogger<NotificationPreferenceService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<NotificationPreferences> GetUserPreferencesAsync(string userId)
    {
        var existing = await _repository.GetByUserIdAsync(userId);
        if (existing != null)
        {
            return existing;
        }

        return new NotificationPreferences
        {
            UserId = userId
        };
    }

    public async Task SetUserPreferencesAsync(string userId, NotificationPreferences preferences)
    {
        preferences.UserId = userId;
        preferences.UpdatedUtc = DateTime.UtcNow;

        await _repository.UpsertAsync(preferences);
        await _repository.SaveChangesAsync();
        _logger.LogInformation("Notification preferences saved for {UserId}", userId);
    }

    public async Task<List<NotificationPreferences>> GetUsersInterestedInAlertAsync(string alertType)
    {
        var all = await _repository.GetAllAsync();

        return all
            .Where(p => p.EmailEnabled)
            .Where(p => p.AlertTypesToNotify.Count == 0 || p.AlertTypesToNotify.Any(t => string.Equals(t, alertType, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }
}
