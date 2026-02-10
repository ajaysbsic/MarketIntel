using Alfanar.MarketIntel.Application.DTOs;
using Alfanar.MarketIntel.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Alfanar.MarketIntel.Application.Services;

/// <summary>
/// Search provider implementation using NewsAPI
/// Great for news, tech updates, and industry insights
/// API: https://newsapi.org/
/// </summary>
public class NewsApiService : IWebSearchProvider
{
    public string ProviderName => "newsapi";

    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<NewsApiService> _logger;

    private string? _apiKey;
    private const string BaseUrl = "https://newsapi.org/v2/everything";

    public NewsApiService(
        IConfiguration configuration,
        HttpClient httpClient,
        ILogger<NewsApiService> logger)
    {
        _configuration = configuration;
        _httpClient = httpClient;
        _logger = logger;

        LoadConfiguration();
    }

    public bool IsConfigured()
    {
        return !string.IsNullOrEmpty(_apiKey);
    }

    public async Task<List<WebSearchResultDto>> SearchAsync(WebSearchRequestDto request)
    {
        var results = new List<WebSearchResultDto>();

        try
        {
            if (!IsConfigured())
                throw new InvalidOperationException("NewsAPI is not configured");

            _logger.LogInformation("Executing NewsAPI search for: {Keyword}", request.Keyword);

            // Build NewsAPI query parameters
            var query = new Dictionary<string, string>
            {
                { "q", request.Keyword },
                { "apiKey", _apiKey! },
                { "pageSize", Math.Min(request.MaxResults, 100).ToString() },
                { "sortBy", "publishedAt" },
                { "language", "en" }
            };

            // Add date filter if provided
            if (request.FromDate.HasValue)
            {
                query["from"] = request.FromDate.Value.ToString("yyyy-MM-dd");
            }

            var queryString = string.Join("&", query.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            var requestUrl = $"{BaseUrl}?{queryString}";

            _logger.LogDebug("Calling NewsAPI: {Url}", requestUrl);

            // NewsAPI requires a User-Agent header
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            httpRequest.Headers.Add("User-Agent", "Alfanar.MarketIntel/1.0");
            
            var response = await _httpClient.SendAsync(httpRequest);
            
            var jsonContent = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("NewsAPI returned {StatusCode}: {Content}", response.StatusCode, jsonContent);
                throw new InvalidOperationException($"NewsAPI error: {response.StatusCode} - {jsonContent}");
            }
            using (var doc = System.Text.Json.JsonDocument.Parse(jsonContent))
            {
                var root = doc.RootElement;

                // Check for API errors
                if (root.TryGetProperty("status", out var statusEl))
                {
                    var status = statusEl.GetString();
                    if (status != "ok")
                    {
                        if (root.TryGetProperty("message", out var msgEl))
                        {
                            var msg = msgEl.GetString();
                            _logger.LogWarning("NewsAPI error: {Message}", msg);
                        }
                        return results;
                    }
                }

                // Parse articles
                if (root.TryGetProperty("articles", out var articlesElement) && articlesElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var article in articlesElement.EnumerateArray())
                    {
                        var title = article.TryGetProperty("title", out var titleEl) ? titleEl.GetString() ?? "" : "";
                        var description = article.TryGetProperty("description", out var descEl) ? descEl.GetString() ?? "" : "";
                        var url = article.TryGetProperty("url", out var urlEl) ? urlEl.GetString() ?? "" : "";
                        var source = article.TryGetProperty("source", out var sourceEl) && sourceEl.TryGetProperty("name", out var nameEl) 
                            ? nameEl.GetString() ?? "" 
                            : "";
                        var publishedAtStr = article.TryGetProperty("publishedAt", out var pubDateEl) ? pubDateEl.GetString() : null;

                        DateTime? publishedDate = null;
                        if (!string.IsNullOrEmpty(publishedAtStr) && DateTime.TryParse(publishedAtStr, out var parsedDate))
                        {
                            publishedDate = parsedDate;
                        }

                        results.Add(new WebSearchResultDto
                        {
                            Id = Guid.NewGuid(),
                            Keyword = request.Keyword,
                            Title = title,
                            Snippet = description,
                            Url = url,
                            Source = source,
                            RetrievedUtc = DateTime.UtcNow,
                            IsFromMonitoring = false,
                            PublishedDate = publishedDate
                        });
                    }

                    _logger.LogInformation("NewsAPI returned {ResultCount} articles for keyword: {Keyword}", results.Count, request.Keyword);
                }
                else
                {
                    _logger.LogWarning("No articles found in NewsAPI response for keyword: {Keyword}", request.Keyword);
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching with NewsAPI for keyword: {Keyword}", request.Keyword);
            throw;
        }
    }

    private void LoadConfiguration()
    {
        _apiKey = _configuration["NewsApi:ApiKey"];

        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("NewsAPI key not configured. Add NewsApi:ApiKey to appsettings.json");
        }
    }
}
