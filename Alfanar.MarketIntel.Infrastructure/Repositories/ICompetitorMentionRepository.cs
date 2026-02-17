using Alfanar.MarketIntel.Domain.Entities;

namespace Alfanar.MarketIntel.Application.Interfaces;

public interface ICompetitorMentionRepository
{
    Task AddAsync(CompetitorMention entity);
    Task AddRangeAsync(List<CompetitorMention> entities);
    Task<List<CompetitorMention>> GetByCompetitorAsync(Guid competitorId);
    Task<List<CompetitorMention>> GetRecentByCompetitorAsync(Guid competitorId, int count = 50);
    Task<bool> ExistsAsync(Guid competitorId, string sourceType, Guid sourceId);
    Task SaveChangesAsync();
}
