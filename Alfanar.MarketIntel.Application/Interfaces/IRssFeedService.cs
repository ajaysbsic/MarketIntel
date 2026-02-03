using Alfanar.MarketIntel.Application.Common;
using Alfanar.MarketIntel.Application.DTOs;

namespace Alfanar.MarketIntel.Application.Interfaces;

public interface IRssFeedService
{
    Task<Result<RssFeedDto>> CreateFeedAsync(RssFeedDto feedDto);
    Task<Result<RssFeedDto>> GetFeedByIdAsync(Guid id);
    Task<Result<List<RssFeedDto>>> GetAllFeedsAsync();
    Task<Result<List<RssFeedDto>>> GetActiveFeedsAsync();
    Task<Result> UpdateFeedAsync(Guid id, RssFeedDto feedDto);
    Task<Result> UpdateFeedLastFetchedAsync(Guid id, string? etag);
    Task<Result> ToggleFeedStatusAsync(Guid id);
    Task<Result> DeleteFeedAsync(Guid id);
}
