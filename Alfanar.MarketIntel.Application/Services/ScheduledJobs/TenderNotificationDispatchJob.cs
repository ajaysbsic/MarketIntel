using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Application.Interfaces.ScheduledJobs;
using Alfanar.MarketIntel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Alfanar.MarketIntel.Application.Services.ScheduledJobs;

public class TenderNotificationDispatchJob : ITenderNotificationDispatchJob
{
    private readonly MarketIntelDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TenderNotificationDispatchJob> _logger;
    private readonly ITenderNotificationBroadcaster _broadcaster;
    private readonly IEmailService _emailService;

    public TenderNotificationDispatchJob(
        MarketIntelDbContext dbContext,
        IConfiguration configuration,
        ILogger<TenderNotificationDispatchJob> logger,
        ITenderNotificationBroadcaster broadcaster,
        IEmailService emailService)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
        _broadcaster = broadcaster;
        _emailService = emailService;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var batchSize = _configuration.GetValue("TenderMonitoring:NotificationDispatchBatchSize", 200);

        var queued = await _dbContext.TenderNotificationLogs
            .Include(x => x.TenderNotice)
                .ThenInclude(n => n.Authority)
            .Include(x => x.TenderNotice)
                .ThenInclude(n => n.Source)
            .Include(x => x.TenderNotice)
                .ThenInclude(n => n.Country)
            .Where(x => x.DeliveryStatus == "Queued")
            .OrderBy(x => x.SentAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        foreach (var item in queued)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var notice = item.TenderNotice;

                if (string.Equals(item.Channel, "InApp", StringComparison.OrdinalIgnoreCase))
                {
                    // Push real-time SignalR event to all connected clients
                    await _broadcaster.BroadcastNewTenderAsync(new
                    {
                        id = notice?.Id,
                        title = item.NotificationTitle ?? notice?.Title,
                        body = item.NotificationBody,
                        authorityName = notice?.Authority?.Name,
                        sector = notice?.Sector,
                        deadline = notice?.Deadline?.ToString("yyyy-MM-dd"),
                        sourceUrl = notice?.SourceUrl,
                        countryIsoCode = notice?.Country?.IsoCode,
                        notificationLogId = item.Id
                    }, cancellationToken);

                    item.DeliveryStatus = "Sent";
                    item.SentAt = DateTime.UtcNow;
                }
                else if (string.Equals(item.Channel, "Email", StringComparison.OrdinalIgnoreCase))
                {
                    var recipient = _configuration["TenderMonitoring:NotificationEmail"]
                        ?? _configuration["Email:DefaultRecipient"];

                    if (!string.IsNullOrWhiteSpace(recipient) && notice != null)
                    {
                        var subject = item.NotificationTitle ?? $"New Tender: {notice.Title}";
                        var body = BuildTenderEmailHtml(notice, item.NotificationBody);
                        var result = await _emailService.SendTenderEmailAsync(recipient, subject, body, cancellationToken);
                        item.DeliveryStatus = result.IsSuccess ? "Sent" : "Failed";
                        item.ProviderMessageId = result.IsSuccess ? "smtp-ok" : result.Error;
                    }
                    else
                    {
                        item.DeliveryStatus = "Skipped_NoRecipient";
                    }

                    item.SentAt = DateTime.UtcNow;
                }
                else
                {
                    item.DeliveryStatus = "Skipped_UnsupportedChannel";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to dispatch tender notification {LogId}", item.Id);
                item.DeliveryStatus = "Failed";
                item.ProviderMessageId = ex.Message;
            }
        }

        if (queued.Count > 0)
        {
            _logger.LogInformation("Tender notification dispatch processed {Count} queued notification(s)", queued.Count);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static string BuildTenderEmailHtml(Domain.Entities.TenderNotice notice, string? body)
    {
        var title = System.Net.WebUtility.HtmlEncode(notice.Title);
        var authority = System.Net.WebUtility.HtmlEncode(notice.Authority?.Name ?? "—");
        var sector = System.Net.WebUtility.HtmlEncode(notice.Sector ?? "—");
        var deadline = notice.Deadline?.ToString("yyyy-MM-dd") ?? "TBD";
        var sourceUrl = System.Net.WebUtility.HtmlEncode(notice.SourceUrl);
        var bodyText = System.Net.WebUtility.HtmlEncode(body ?? notice.Summary ?? "No summary available.");

        return $"""
            <html><body style="font-family:Arial,sans-serif;color:#111827;max-width:600px;margin:auto;padding:24px">
              <div style="background:#1f47ba;color:#fff;padding:16px 20px;border-radius:8px 8px 0 0">
                <h2 style="margin:0">📋 New Saudi Tender Alert</h2>
              </div>
              <div style="border:1px solid #e5e7eb;border-top:none;padding:20px;border-radius:0 0 8px 8px">
                <h3 style="margin:0 0 12px;color:#1f47ba">{title}</h3>
                <table style="width:100%;font-size:14px;border-collapse:collapse">
                  <tr><td style="padding:6px 0;color:#6b7280;width:120px">Authority</td><td><strong>{authority}</strong></td></tr>
                  <tr><td style="padding:6px 0;color:#6b7280">Sector</td><td>{sector}</td></tr>
                  <tr><td style="padding:6px 0;color:#6b7280">Deadline</td><td><strong style="color:#b91c1c">{deadline}</strong></td></tr>
                </table>
                <p style="color:#4b5563;margin:16px 0 20px">{bodyText}</p>
                <a href="{sourceUrl}" style="background:#1f47ba;color:#fff;padding:10px 20px;border-radius:6px;text-decoration:none;font-weight:600">View Tender →</a>
                <p style="font-size:12px;color:#9ca3af;margin-top:20px">Alfanar Market Intelligence · Saudi Tender Monitor</p>
              </div>
            </body></html>
            """;
    }
}
