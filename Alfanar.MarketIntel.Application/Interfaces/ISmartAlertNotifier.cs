using Alfanar.MarketIntel.Domain.Entities;

namespace Alfanar.MarketIntel.Application.Interfaces;

public interface ISmartAlertNotifier
{
    Task NotifyAsync(SmartAlert alert);
}
