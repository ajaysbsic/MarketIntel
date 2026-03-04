namespace Alfanar.MarketIntel.Application.Interfaces;

public interface ITenderEventPublisher
{
    Task PublishTenderVersionCreatedAsync(Guid tenderNoticeId, Guid tenderVersionId, string changeType, DateTime detectedAt);
}
