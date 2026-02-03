using Microsoft.AspNetCore.SignalR;

namespace Alfanar.MarketIntel.Api.Hubs;

/// <summary>
/// SignalR hub for real-time notifications
/// Supports both news articles and financial reports
/// </summary>
public class NotificationsHub : Hub
{
    private readonly ILogger<NotificationsHub> _logger;

    public NotificationsHub(ILogger<NotificationsHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Send a generic notification to all clients
    /// </summary>
    public async Task SendNotification(string message)
    {
        await Clients.All.SendAsync("ReceiveNotification", message);
    }

    /// <summary>
    /// Notify clients about a new article
    /// Called automatically by NewsController after ingestion
    /// </summary>
    public async Task NotifyNewArticle(object article)
    {
        await Clients.All.SendAsync("newArticle", article);
        _logger.LogInformation("New article notification sent to all clients");
    }

    /// <summary>
    /// Notify clients about a new financial report
    /// Called automatically by ReportsController after ingestion
    /// </summary>
    public async Task NotifyNewReport(object report)
    {
        await Clients.All.SendAsync("newReport", report);
        _logger.LogInformation("New report notification sent to all clients");
    }

    /// <summary>
    /// Notify clients when report analysis is complete
    /// Called after AI analysis finishes
    /// </summary>
    public async Task NotifyReportAnalysisComplete(Guid reportId, object analysis)
    {
        await Clients.All.SendAsync("reportAnalysisComplete", new { reportId, analysis });
        _logger.LogInformation("Report analysis complete notification sent for report {ReportId}", reportId);
    }

    /// <summary>
    /// Notify clients about report processing status update
    /// </summary>
    public async Task NotifyReportStatusUpdate(Guid reportId, string status, string? message = null)
    {
        await Clients.All.SendAsync("reportStatusUpdate", new { reportId, status, message });
        _logger.LogDebug("Report status update sent for report {ReportId}: {Status}", reportId, status);
    }

    /// <summary>
    /// Join a specific company channel to receive updates for that company only
    /// </summary>
    public async Task JoinCompanyChannel(string companyName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"company_{companyName}");
        _logger.LogInformation("Client {ConnectionId} joined company channel: {Company}", Context.ConnectionId, companyName);
    }

    /// <summary>
    /// Leave a company channel
    /// </summary>
    public async Task LeaveCompanyChannel(string companyName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"company_{companyName}");
        _logger.LogInformation("Client {ConnectionId} left company channel: {Company}", Context.ConnectionId, companyName);
    }

    /// <summary>
    /// Send notification to a specific company channel
    /// </summary>
    public async Task NotifyCompanyChannel(string companyName, string eventType, object data)
    {
        await Clients.Group($"company_{companyName}").SendAsync(eventType, data);
        _logger.LogDebug("Notification sent to company channel: {Company}", companyName);
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        _logger.LogInformation("Client connected: {ConnectionId} from {UserAgent}", 
            Context.ConnectionId, 
            Context.GetHttpContext()?.Request.Headers["User-Agent"]);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
        
        if (exception != null)
        {
            _logger.LogWarning(exception, "Client disconnected with error: {ConnectionId}", Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        }
    }
}
