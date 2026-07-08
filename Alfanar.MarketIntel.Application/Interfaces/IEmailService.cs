using Alfanar.MarketIntel.Application.Common;
using Alfanar.MarketIntel.Domain.Entities;

namespace Alfanar.MarketIntel.Application.Interfaces;

public interface IEmailService
{
    Task<Result<bool>> SendAlertEmailAsync(string recipient, SmartAlert alert, CancellationToken cancellationToken = default);
    Task<Result<bool>> SendDigestEmailAsync(string recipient, List<SmartAlert> alerts, CancellationToken cancellationToken = default);
    Task<Result<bool>> SendTenderEmailAsync(string recipient, string subject, string bodyHtml, CancellationToken cancellationToken = default);
}
