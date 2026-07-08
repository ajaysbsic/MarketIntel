using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Alfanar.MarketIntel.Api.Services;

/// <summary>
/// Implements ITenderNotificationBroadcaster by sending real-time events
/// via the NotificationsHub SignalR hub.
/// </summary>
public sealed class SignalRTenderBroadcaster : ITenderNotificationBroadcaster
{
    private readonly IHubContext<NotificationsHub> _hubContext;

    public SignalRTenderBroadcaster(IHubContext<NotificationsHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task BroadcastNewTenderAsync(object payload, CancellationToken ct = default)
        => _hubContext.Clients.All.SendAsync("newTender", payload, ct);
}
