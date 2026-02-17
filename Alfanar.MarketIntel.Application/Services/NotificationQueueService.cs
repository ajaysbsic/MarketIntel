using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Domain.Entities;
using Alfanar.MarketIntel.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Alfanar.MarketIntel.Application.Services;

public class NotificationQueueService : INotificationQueueService
{
    private readonly INotificationQueueRepository _queueRepository;
    private readonly ISmartAlertRepository _alertRepository;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificationQueueService> _logger;

    public NotificationQueueService(
        INotificationQueueRepository queueRepository,
        ISmartAlertRepository alertRepository,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<NotificationQueueService> logger)
    {
        _queueRepository = queueRepository;
        _alertRepository = alertRepository;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task EnqueueNotificationAsync(SmartAlert alert, NotificationPreferences preferences)
    {
        if (!ShouldNotify(alert, preferences))
        {
            return;
        }

        var queueItem = new NotificationQueue
        {
            AlertId = alert.Id,
            UserId = preferences.UserId,
            NotificationType = "Email",
            Recipient = preferences.EmailAddress ?? string.Empty,
            Status = NotificationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _queueRepository.AddAsync(queueItem);
        await _queueRepository.SaveChangesAsync();
        _logger.LogInformation("Notification queued for alert {AlertId} to user {UserId}", alert.Id, preferences.UserId);
    }

    public async Task ProcessNotificationQueueAsync(CancellationToken cancellationToken = default)
    {
        var pendingItems = await _queueRepository.GetPendingAsync(batchSize: 100);
        if (pendingItems.Count == 0)
        {
            return;
        }

        var maxRetries = _configuration.GetValue("Notifications:MaxRetries", 3);

        foreach (var item in pendingItems)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                item.Status = NotificationStatus.Processing;
                await _queueRepository.UpdateAsync(item);
                await _queueRepository.SaveChangesAsync();

                var alert = await _alertRepository.GetByIdAsync(item.AlertId);
                if (alert == null)
                {
                    item.Status = NotificationStatus.Failed;
                    item.ErrorMessage = "Alert not found";
                    await _queueRepository.UpdateAsync(item);
                    await _queueRepository.SaveChangesAsync();
                    continue;
                }

                var result = await _emailService.SendAlertEmailAsync(item.Recipient, alert, cancellationToken);
                if (result.IsSuccess)
                {
                    item.Status = NotificationStatus.Sent;
                    item.SentAt = DateTime.UtcNow;
                    item.ErrorMessage = null;
                }
                else
                {
                    item.RetryCount++;
                    item.ErrorMessage = result.Error;
                    item.Status = item.RetryCount >= maxRetries ? NotificationStatus.Failed_MaxRetries : NotificationStatus.Pending;
                }
            }
            catch (Exception ex)
            {
                item.RetryCount++;
                item.ErrorMessage = ex.Message;
                item.Status = item.RetryCount >= maxRetries ? NotificationStatus.Failed_MaxRetries : NotificationStatus.Pending;
            }

            await _queueRepository.UpdateAsync(item);
            await _queueRepository.SaveChangesAsync();
        }
    }

    private static bool ShouldNotify(SmartAlert alert, NotificationPreferences preferences)
    {
        if (!preferences.EmailEnabled)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(preferences.EmailAddress))
        {
            return false;
        }

        if (alert.Severity == "Critical" && !preferences.NotifyOnCritical)
        {
            return false;
        }

        if (alert.Severity == "High" && !preferences.NotifyOnHigh)
        {
            return false;
        }

        if (alert.Severity == "Medium" && !preferences.NotifyOnMedium)
        {
            return false;
        }

        if (preferences.AlertTypesToNotify.Count > 0 &&
            !preferences.AlertTypesToNotify.Any(t => string.Equals(t, alert.AlertType, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (preferences.KeywordsToNotify.Count == 0)
        {
            return true;
        }

        var keywords = (alert.TriggerKeywords ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(k => k.Trim())
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .ToList();

        return preferences.KeywordsToNotify.Any(k =>
            keywords.Any(t => string.Equals(t, k, StringComparison.OrdinalIgnoreCase)) ||
            alert.Title.Contains(k, StringComparison.OrdinalIgnoreCase) ||
            alert.Message.Contains(k, StringComparison.OrdinalIgnoreCase));
    }
}
