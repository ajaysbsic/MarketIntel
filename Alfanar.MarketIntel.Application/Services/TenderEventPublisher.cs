using Alfanar.MarketIntel.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Alfanar.MarketIntel.Application.Services;

public class TenderEventPublisher : ITenderEventPublisher
{
    private readonly ILogger<TenderEventPublisher> _logger;

    public TenderEventPublisher(ILogger<TenderEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishTenderVersionCreatedAsync(Guid tenderNoticeId, Guid tenderVersionId, string changeType, DateTime detectedAt)
    {
        _logger.LogInformation(
            "TenderVersionCreated event published. TenderNoticeId={TenderNoticeId}, TenderVersionId={TenderVersionId}, ChangeType={ChangeType}, DetectedAt={DetectedAt}",
            tenderNoticeId,
            tenderVersionId,
            changeType,
            detectedAt);

        return Task.CompletedTask;
    }
}
