using System.Net;
using System.Net.Mail;
using Alfanar.MarketIntel.Application.Common;
using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Alfanar.MarketIntel.Application.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Result<bool>> SendAlertEmailAsync(string recipient, SmartAlert alert, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_configuration.GetValue("Email:Enabled", true))
            {
                return Result<bool>.Success(false);
            }

            using var smtpClient = BuildSmtpClient();
            using var mailMessage = new MailMessage
            {
                From = new MailAddress(_configuration["Email:FromAddress"] ?? "alerts@alfanar.com"),
                Subject = $"[{alert.Severity}] {alert.Title}",
                Body = BuildAlertEmailBody(alert),
                IsBodyHtml = true
            };
            mailMessage.To.Add(recipient);

            await smtpClient.SendMailAsync(mailMessage, cancellationToken);
            _logger.LogInformation("Alert email sent to {Recipient} for alert {AlertId}", recipient, alert.Id);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send alert email to {Recipient}", recipient);
            return Result<bool>.Failure(ex.Message);
        }
    }

    public Task<Result<bool>> SendDigestEmailAsync(string recipient, List<SmartAlert> alerts, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result<bool>.Failure("Digest emails not implemented"));
    }

    public async Task<Result<bool>> SendTenderEmailAsync(string recipient, string subject, string bodyHtml, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_configuration.GetValue("Email:Enabled", true))
                return Result<bool>.Success(false);

            using var smtpClient = BuildSmtpClient();
            using var mail = new MailMessage
            {
                From = new MailAddress(_configuration["Email:FromAddress"] ?? "tenders@alfanar.com"),
                Subject = subject,
                Body = bodyHtml,
                IsBodyHtml = true
            };
            mail.To.Add(recipient);
            await smtpClient.SendMailAsync(mail, cancellationToken);
            _logger.LogInformation("Tender email sent to {Recipient}", recipient);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send tender email to {Recipient}", recipient);
            return Result<bool>.Failure(ex.Message);
        }
    }

    private SmtpClient BuildSmtpClient()
    {
        var host = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
        var port = _configuration.GetValue("Email:SmtpPort", 587);
        var username = _configuration["Email:Username"];
        var password = _configuration["Email:Password"];

        return new SmtpClient(host)
        {
            Port = port,
            Credentials = string.IsNullOrWhiteSpace(username) ? CredentialCache.DefaultNetworkCredentials : new NetworkCredential(username, password),
            EnableSsl = true
        };
    }

    private static string BuildAlertEmailBody(SmartAlert alert)
    {
        var safeMessage = WebUtility.HtmlEncode(alert.Message).Replace("\n", "<br/>");
        var safeTitle = WebUtility.HtmlEncode(alert.Title);
        var safeType = WebUtility.HtmlEncode(alert.AlertType);
        var safeCompany = WebUtility.HtmlEncode(alert.CompanyName);
        var safeKeywords = WebUtility.HtmlEncode(alert.TriggerKeywords ?? string.Empty);
        var safeSourceUrl = WebUtility.HtmlEncode(alert.SourceUrl ?? string.Empty);

        var sourceLink = string.IsNullOrWhiteSpace(safeSourceUrl)
            ? string.Empty
            : $"<p><a href='{safeSourceUrl}'>View Source</a></p>";

        return $@"
<div style='font-family: Arial, sans-serif; max-width: 600px;'>
  <div style='background-color: #b71c1c; color: white; padding: 16px; text-align: center;'>
    <h2 style='margin: 0;'>[{alert.Severity}] {safeTitle}</h2>
  </div>
  <div style='padding: 16px; border: 1px solid #ddd;'>
    <p><strong>Type:</strong> {safeType}</p>
    <p><strong>Company:</strong> {safeCompany}</p>
    <p><strong>Message:</strong></p>
    <p>{safeMessage}</p>
    <p><strong>Triggered Keywords:</strong> {safeKeywords}</p>
    {sourceLink}
  </div>
</div>";
    }
}
