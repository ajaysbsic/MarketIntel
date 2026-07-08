namespace Alfanar.MarketIntel.Application.Interfaces;

/// <summary>
/// Abstracts real-time delivery of tender notifications so the Application layer
/// does not depend directly on SignalR infrastructure types.
/// </summary>
public interface ITenderNotificationBroadcaster
{
    Task BroadcastNewTenderAsync(object payload, CancellationToken ct = default);
}
