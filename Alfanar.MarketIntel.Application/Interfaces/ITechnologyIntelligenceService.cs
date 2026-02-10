using Alfanar.MarketIntel.Application.Common;
using Alfanar.MarketIntel.Application.DTOs;

namespace Alfanar.MarketIntel.Application.Interfaces;

public interface ITechnologyIntelligenceService
{
    Task<Result<TechnologyOverviewDto>> GetOverviewAsync(TechnologyIntelligenceFilterDto filter);
    Task<Result<List<TechnologyTrendPointDto>>> GetTimelineAsync(TechnologyIntelligenceFilterDto filter);
    Task<Result<List<TechnologyRegionSignalDto>>> GetRegionsAsync(TechnologyIntelligenceFilterDto filter);
    Task<Result<List<TechnologyKeyPlayerDto>>> GetKeyPlayersAsync(TechnologyIntelligenceFilterDto filter, int maxItems = 10);
    Task<Result<List<TechnologyInsightDto>>> GetInsightsAsync(TechnologyIntelligenceFilterDto filter);
    Task<Result<TechnologyIntelligenceSummaryDto>> GetSummaryAsync(TechnologyIntelligenceFilterDto filter);
}
