using Alfanar.MarketIntel.Domain.Entities;

namespace Alfanar.MarketIntel.Application.Interfaces;

public interface INotificationQueueService
{
    Task EnqueueNotificationAsync(SmartAlert alert, NotificationPreferences preferences);
    Task ProcessNotificationQueueAsync(CancellationToken cancellationToken = default);
}
