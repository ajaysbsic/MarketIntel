using Alfanar.MarketIntel.Application.DTOs;
using Alfanar.MarketIntel.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Alfanar.MarketIntel.Application.Services;

/// <summary>
/// Search provider implementation using Google Custom Search API
/// Implements IWebSearchProvider for abstraction layer (plug-and-play provider)
/// </summary>
public class GoogleSearchService : IWebSearchProvider
{
    public string ProviderName => "google";

    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<GoogleSearchService> _logger;

    private string? _apiKey;
    private string? _searchEngineId;

    public GoogleSearchService(
        IConfiguration configuration,
        HttpClient httpClient,
        ILogger<GoogleSearchService> logger)
    {
        _configuration = configuration;
        _httpClient = httpClient;
        _logger = logger;

        LoadConfiguration();
    }

    public bool IsConfigured()
    {
        return !string.IsNullOrEmpty(_apiKey) && !string.IsNullOrEmpty(_searchEngineId);
    }

    public async Task<List<WebSearchResultDto>> SearchAsync(WebSearchRequestDto request)
    {
        var results = new List<WebSearchResultDto>();

        try
        {
            if (!IsConfigured())
                throw new InvalidOperationException("Google Search API is not configured");

            _logger.LogInformation("Executing Google Custom Search for: {Keyword}", request.Keyword);

            // Construct the Google Custom Search API URL
            var url = "https://www.googleapis.com/customsearch/v1";
            var query = new Dictionary<string, string>
            {
                { "q", request.Keyword },
                { "key", _apiKey! },
                { "cx", _searchEngineId! },
                { "num", Math.Min(request.MaxResults, 10).ToString() } // Google CSE max 10 per request
            };

            var queryString = string.Join("&", query.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            var requestUrl = $"{url}?{queryString}";

            _logger.LogDebug("Calling Google Custom Search API: {Url}", url);

            var response = await _httpClient.GetAsync(requestUrl);
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync();
            using (var doc = System.Text.Json.JsonDocument.Parse(jsonContent))
            {
                var root = doc.RootElement;

                if (root.TryGetProperty("items", out var itemsElement) && itemsElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    var index = 0;
                    foreach (var item in itemsElement.EnumerateArray())
                    {
                        index++;
                        
                        var title = item.TryGetProperty("title", out var titleEl) ? titleEl.GetString() ?? "" : "";
                        var snippet = item.TryGetProperty("snippet", out var snippetEl) ? snippetEl.GetString() ?? "" : "";
                        var url_str = item.TryGetProperty("link", out var urlEl) ? urlEl.GetString() ?? "" : "";
                        var displayLink = item.TryGetProperty("displayLink", out var displayEl) ? displayEl.GetString() ?? "" : "";

                        results.Add(new WebSearchResultDto
                        {
                            Id = Guid.NewGuid(),
                            Keyword = request.Keyword,
                            Title = title,
                            Snippet = snippet,
                            Url = url_str,
                            Source = displayLink,
                            RetrievedUtc = DateTime.UtcNow,
                            IsFromMonitoring = false,
                            PublishedDate = null // Google CSE doesn't provide published date
                        });
                    }

                    _logger.LogInformation("Google Custom Search returned {ResultCount} results for keyword: {Keyword}", results.Count, request.Keyword);
                }
                else
                {
                    _logger.LogWarning("No search results found in Google Custom Search response for keyword: {Keyword}", request.Keyword);
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching with Google for keyword: {Keyword}", request.Keyword);
            throw;
        }
    }

    private void LoadConfiguration()
    {
        _apiKey = _configuration["GoogleSearch:ApiKey"];
        _searchEngineId = _configuration["GoogleSearch:SearchEngineId"];

        if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_searchEngineId))
        {
            _logger.LogWarning("Google Search API credentials not configured. Add GoogleSearch:ApiKey and GoogleSearch:SearchEngineId to appsettings.json");
        }
    }
}
