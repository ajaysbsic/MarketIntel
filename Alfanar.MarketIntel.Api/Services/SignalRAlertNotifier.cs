using Alfanar.MarketIntel.Api.Hubs;
using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Domain.Entities;
using Microsoft.AspNetCore.SignalR;

namespace Alfanar.MarketIntel.Api.Services;

public class SignalRAlertNotifier : ISmartAlertNotifier
{
    private readonly IHubContext<NotificationsHub> _hub;

    public SignalRAlertNotifier(IHubContext<NotificationsHub> hub)
    {
        _hub = hub;
    }

    public async Task NotifyAsync(SmartAlert alert)
    {
        await _hub.Clients.All.SendAsync("smartAlert", new
        {
            alert.Id,
            alert.AlertType,
            alert.Severity,
            alert.Title,
            alert.Message,
            alert.CompanyName,
            alert.SourceType,
            alert.SourceId,
            alert.SourceUrl,
            alert.CreatedAt
        });
    }
}
