using Alfanar.MarketIntel.Application.Common;
using Alfanar.MarketIntel.Application.DTOs;

namespace Alfanar.MarketIntel.Application.Interfaces;

public interface ICompetitorTrackingService
{
    Task<Result<CompetitorDto>> AddCompetitorAsync(CreateCompetitorDto dto);
    Task<Result<CompetitorDto>> UpdateCompetitorAsync(Guid id, CreateCompetitorDto dto);
    Task<Result<bool>> DeleteCompetitorAsync(Guid id);
    Task<Result<List<CompetitorDto>>> GetCompetitorsAsync(bool includeInactive = true);
    Task<Result<List<CompetitorDto>>> GetAutoDetectedCompetitorsAsync();
    Task<Result<CompetitorDashboardDto>> GetCompetitorDashboardAsync(Guid id);
    Task<Result<CompetitorComparisonDto>> CompareCompetitorsAsync(List<Guid> ids);
    Task<Result<List<CompetitorMentionDto>>> ScanForMentionsAsync(Guid competitorId);
    Task<Result<CompetitorDetectionResultDto>> AutoDetectCompetitorsAsync(string articleText);
    Task<Result<List<CompetitorMentionDto>>> ScanArticleAsync(CompetitorScanRequestDto request);
}
