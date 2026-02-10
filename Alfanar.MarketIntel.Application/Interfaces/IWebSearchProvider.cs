using Alfanar.MarketIntel.Application.DTOs;

namespace Alfanar.MarketIntel.Application.Interfaces;

/// <summary>
/// Abstraction layer for search providers (Google, Bing, SerpAPI, etc.)
/// Allows swapping search providers without changing consumer code
/// </summary>
public interface IWebSearchProvider
{
    /// <summary>Gets the name of this search provider</summary>
    string ProviderName { get; }

    /// <summary>Checks if the provider has been properly configured with required credentials</summary>
    bool IsConfigured();

    /// <summary>Executes a web search and returns results</summary>
    Task<List<WebSearchResultDto>> SearchAsync(WebSearchRequestDto request);
}
