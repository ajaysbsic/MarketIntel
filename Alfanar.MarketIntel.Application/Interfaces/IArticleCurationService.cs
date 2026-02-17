using Alfanar.MarketIntel.Application.Common;
using Alfanar.MarketIntel.Application.DTOs;

namespace Alfanar.MarketIntel.Application.Interfaces;

public interface IArticleCurationService
{
    /// <summary>
    /// Curate and summarize web search results into clustered intelligence
    /// </summary>
    Task<Result<CuratedIntelligenceDto>> CurateArticlesAsync(List<WebSearchResultDto> articles, string keyword);
}
