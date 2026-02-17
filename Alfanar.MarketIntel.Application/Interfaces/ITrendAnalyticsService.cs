using Alfanar.MarketIntel.Application.Common;
using Alfanar.MarketIntel.Application.DTOs;

namespace Alfanar.MarketIntel.Application.Interfaces;

public interface ITrendAnalyticsService
{
    Task<Result<bool>> GenerateDailySnapshotAsync(DateTime date);
    Task<Result<List<TrendPointDto>>> GetKeywordTrendAsync(string keyword, int days);
    Task<Result<List<CompetitorVisibilityPointDto>>> GetCompetitorVisibilityAsync(Guid competitorId, int days);
    Task<Result<List<NoiseSignalPointDto>>> GetMarketNoiseVsSignalAsync(string keyword, int days);
    Task<Result<TrendComparisonDto>> GetTrendComparisonAsync(List<string> keywords, int days);
    Task<Result<WeeklyDigestDto>> GetWeeklyDigestAsync();
}
