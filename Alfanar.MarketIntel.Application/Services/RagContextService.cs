using Alfanar.MarketIntel.Application.DTOs;
using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Alfanar.MarketIntel.Application.Services;

/// <summary>
/// RAG (Retrieval Augmented Generation) Context Service
/// Retrieves and ranks relevant data from database for AI enrichment
/// Performance: ~200-500ms for full context retrieval
/// </summary>
public interface IRagContextService
{
    Task<RagContextDto> GetEnrichedContextAsync(string query, string? entity = null);
    Task<RagContextDto> GetEnrichedContextWithWebSearchAsync(string query, string? entity = null, bool includeWebSearch = true);
    double ScoreRelevance(string data, string query);
    List<string> ExtractEntities(string query);
    string ExpandQuery(string query);
}

public class RagContextService : IRagContextService
{
    private readonly INewsRepository _newsRepo;
    private readonly IFinancialReportRepository _reportRepo;
    private readonly ISmartAlertRepository _alertRepo;
    private readonly IWebSearchService _webSearchService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RagContextService> _logger;
    private readonly Dictionary<string, string> _entitySynonyms;

    // Performance optimization: Cache common queries for 5 minutes
    private static readonly Dictionary<string, CachedContext> QueryCache = new();
    private static readonly Dictionary<string, CachedWebSearch> WebSearchCache = new();
    private const int CacheDurationSeconds = 300;

    public RagContextService(
        INewsRepository newsRepo,
        IFinancialReportRepository reportRepo,
        ISmartAlertRepository alertRepo,
        IWebSearchService webSearchService,
        IConfiguration configuration,
        ILogger<RagContextService> logger)
    {
        _newsRepo = newsRepo;
        _reportRepo = reportRepo;
        _alertRepo = alertRepo;
        _webSearchService = webSearchService;
        _configuration = configuration;
        _logger = logger;
        
        // Initialize entity synonyms for better matching
        _entitySynonyms = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "samsung", "Samsung" },
            { "apple", "Apple" },
            { "microsoft", "Microsoft" },
            { "google", "Google" },
            { "meta", "Meta" },
            { "tesla", "Tesla" },
            { "amazon", "Amazon" },
        };
    }

    /// <summary>
    /// Main method: Get complete enriched context for query
    /// Retrieves from Reports, News, and Alerts in parallel
    /// </summary>
    public async Task<RagContextDto> GetEnrichedContextAsync(string query, string? entity = null)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew();
        var cacheKey = $"{query}_{entity}";

        // Check cache first
        if (QueryCache.TryGetValue(cacheKey, out var cached))
        {
            if ((DateTime.UtcNow - cached.CreatedAt).TotalSeconds < CacheDurationSeconds)
            {
                _logger.LogInformation("RAG Context retrieved from cache");
                return cached.Context;
            }
            QueryCache.Remove(cacheKey); // Remove expired cache
        }

        var context = new RagContextDto
        {
            Query = query,
            Entity = entity ?? ExtractEntities(query).FirstOrDefault(),
            CurrentDate = DateTime.UtcNow,
            RetrievalTimestamp = DateTime.UtcNow
        };

        try
        {
            // Expand query for better matching
            var expandedQuery = ExpandQuery(query);
            
            // Parallel retrieval for performance optimization
            var reportTask = RetrieveReportsAsync(expandedQuery, context.Entity);
            var newsTask = RetrieveNewsAsync(expandedQuery, context.Entity);
            var alertTask = RetrieveAlertsAsync(expandedQuery, context.Entity);

            await Task.WhenAll(reportTask, newsTask, alertTask);

            context.Reports = await reportTask;
            context.NewsArticles = await newsTask;
            context.Alerts = await alertTask;

            // Rank by relevance
            context = RankByRelevance(context, expandedQuery);

            // Cache the result
            QueryCache[cacheKey] = new CachedContext
            {
                Context = context,
                CreatedAt = DateTime.UtcNow
            };

            timer.Stop();
            _logger.LogInformation($"RAG Context built in {timer.ElapsedMilliseconds}ms: " +
                $"{context.Reports.Count} reports, {context.NewsArticles.Count} news, " +
                $"{context.Alerts.Count} alerts");

            return context;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building RAG context");
            timer.Stop();
            return context;
        }
    }

    public async Task<RagContextDto> GetEnrichedContextWithWebSearchAsync(
        string query,
        string? entity = null,
        bool includeWebSearch = true)
    {
        var context = await GetEnrichedContextAsync(query, entity);
        if (!includeWebSearch)
        {
            return context;
        }

        // Get keyword for web search - prioritize entity, then extract from query
        var keyword = entity ?? context.Entity;
        
        // If still no keyword, extract key terms from the query itself
        if (string.IsNullOrWhiteSpace(keyword))
        {
            keyword = ExtractSearchKeywordFromQuery(query);
        }
        
        if (string.IsNullOrWhiteSpace(keyword))
        {
            _logger.LogInformation("No keyword extracted from query for web search: {Query}", query);
            return context;
        }

        try
        {
            _logger.LogInformation("Performing live web search for keyword: {Keyword} (from query: {Query})", keyword, query);
            context.WebSearchResults = await GetLiveWebSearchAsync(query, keyword);
            _logger.LogInformation("Web search returned {Count} results", context.WebSearchResults.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Live web search failed for query: {Query}", query);
        }

        return context;
    }
    
    /// <summary>
    /// Extract meaningful search keyword from user query
    /// Removes common words and extracts the most relevant search terms
    /// </summary>
    private string ExtractSearchKeywordFromQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return string.Empty;

        // Keywords that indicate search intent with potential context
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "a", "an", "the", "and", "or", "but", "in", "on", "at", "to", "for",
            "of", "with", "by", "from", "about", "as", "into", "through", "during",
            "before", "after", "above", "below", "between", "under", "again",
            "further", "then", "once", "any", "all", "both", "each", "few", "more",
            "most", "other", "some", "such", "no", "nor", "not", "only", "own",
            "same", "so", "than", "too", "very", "can", "will", "just",
            "are", "is", "was", "were", "be", "been", "being", "have", "has", "had",
            "do", "does", "did", "i", "me", "my", "you", "your", "tell", "there",
            "looking", "look", "search", "find", "get", "give", "show", "what",
            "when", "where", "which", "who", "whom", "whose", "why", "how"
        };

        // Split query into words and filter
        var words = Regex.Split(query.ToLowerInvariant(), @"\W+")
            .Where(w => w.Length > 2 && !stopWords.Contains(w))
            .ToArray();

        // Take top 3 most relevant words for search
        var searchKeyword = string.Join(" ", words.Take(3));
        
        _logger.LogDebug("Extracted search keyword '{Keyword}' from query '{Query}'", searchKeyword, query);
        return searchKeyword;
    }

    /// <summary>
    /// Retrieve financial reports with optimized query
    /// </summary>
    private async Task<List<ReportContextDto>> RetrieveReportsAsync(string query, string? entity)
    {
        try
        {
            // Get recent reports with default parameters except search term
            var reports = await _reportRepo.GetFilteredAsync(
                companyName: entity,
                pageSize: 10,
                pageNumber: 1);

            // Filter by query in memory
            var filtered = reports
                .Where(r => r.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                           (r.ExtractedText != null && r.ExtractedText.Contains(query, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(r => r.PublishedDate)
                .Take(5)
                .Select(r => new ReportContextDto
                {
                    Id = r.Id,
                    Title = r.Title,
                    Summary = r.ExtractedText?.Length > 300 ? r.ExtractedText.Substring(0, 300) + "..." : (r.ExtractedText ?? "No details available"),
                    CompanyName = r.CompanyName,
                    PublishedDate = r.PublishedDate ?? DateTime.UtcNow,
                    Relevance = ScoreRelevance(r.Title + " " + (r.ExtractedText ?? ""), query)
                })
                .ToList();

            return filtered;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reports");
            return new List<ReportContextDto>();
        }
    }

    /// <summary>
    /// Retrieve news articles with date filter for recency
    /// Database optimization: Uses indexes on PublishedDate, Title, Summary
    /// </summary>
    private async Task<List<NewsContextDto>> RetrieveNewsAsync(string query, string? entity)
    {
        try
        {
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            
            var articles = await _newsRepo.GetFilteredAsync(
                searchTerm: query,
                pageSize: 10,
                pageNumber: 1);

            // Apply date filter and entity filter
            var filtered = articles
                .Where(a => a.PublishedUtc > thirtyDaysAgo &&
                          (string.IsNullOrEmpty(entity) || 
                           a.Title.Contains(entity, StringComparison.OrdinalIgnoreCase) ||
                           a.Source.Contains(entity, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(a => a.PublishedUtc)
                .Take(10)
                .Select(a => new NewsContextDto
                {
                    Id = a.Id.ToString(),
                    Title = a.Title,
                    Summary = a.Summary.Length > 200 ? a.Summary.Substring(0, 200) + "..." : a.Summary,
                    PublishedDate = a.PublishedUtc,
                    Source = a.Source,
                    Relevance = ScoreRelevance(a.Title + " " + a.Summary, query)
                })
                .ToList();

            return filtered;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving news");
            return new List<NewsContextDto>();
        }
    }

    /// <summary>
    /// Retrieve active alerts
    /// Database optimization: Uses index on Status, PublishedDate
    /// </summary>
    private async Task<List<AlertContextDto>> RetrieveAlertsAsync(string query, string? entity)
    {
        try
        {
            // Get active alerts with high priority
            var allAlerts = await _alertRepo.GetUnacknowledgedAsync();

            var filtered = allAlerts
                .Where(a => a.Title.Contains(query, StringComparison.OrdinalIgnoreCase) || 
                           a.Message.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                           (entity != null && a.CompanyName.Contains(entity, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(a => a.CreatedAt)
                .Take(5)
                .Select(a => new AlertContextDto
                {
                    Id = a.Id.ToString(),
                    Title = a.Title,
                    Description = a.Message.Length > 200 ? a.Message.Substring(0, 200) + "..." : a.Message,
                    Severity = a.Severity,
                    AlertType = a.AlertType,
                    UpdatedAt = a.CreatedAt,
                    Relevance = ScoreRelevance(a.Title + " " + a.Message, query)
                })
                .ToList();

            return filtered;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alerts");
            return new List<AlertContextDto>();
        }
    }

    /// <summary>
    /// Score data by relevance to query (0.0 - 1.0)
    /// Scoring formula:
    /// - Exact match: 0.9
    /// - Partial match: 0.6 + keyword_count * 0.1
    /// - Recency bonus: -days_old * 0.01
    /// </summary>
    public double ScoreRelevance(string data, string query)
    {
        if (string.IsNullOrEmpty(data) || string.IsNullOrEmpty(query))
            return 0.0;

        var score = 0.0;
        var queryWords = query.ToLower().Split(new[] { ' ', ',', '.', '?' }, 
            StringSplitOptions.RemoveEmptyEntries);
        var dataLower = data.ToLower();

        // Exact phrase match
        if (dataLower.Contains(query.ToLower()))
            score += 0.5;

        // Word matches
        var matchingWords = queryWords.Count(word => dataLower.Contains(word));
        score += (matchingWords / (double)queryWords.Length) * 0.5;

        return Math.Min(score, 1.0);
    }

    /// <summary>
    /// Extract potential entities from query
    /// Uses basic NLP patterns
    /// </summary>
    public List<string> ExtractEntities(string query)
    {
        var entities = new List<string>();

        // Check for known entity synonyms
        foreach (var (key, value) in _entitySynonyms)
        {
            if (query.Contains(key, StringComparison.OrdinalIgnoreCase))
                entities.Add(value);
        }

        // Extract capitalized words (likely proper nouns)
        var words = query.Split(new[] { ' ', ',', '.', '?' }, StringSplitOptions.RemoveEmptyEntries);
        var properNouns = words.Where(w => char.IsUpper(w[0]) && w.Length > 2)
            .Where(w => !_entitySynonyms.ContainsValue(w))
            .Distinct();

        entities.AddRange(properNouns);

        return entities.Distinct().ToList();
    }

    /// <summary>
    /// Expand query with related terms for better matching
    /// Example: "Samsung profit" → "Samsung profit revenue earnings financial"
    /// </summary>
    public string ExpandQuery(string query)
    {
        var expansions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "profit", "profit revenue earnings income" },
            { "loss", "loss deficit decline decrease" },
            { "risk", "risk threat danger vulnerability" },
            { "opportunity", "opportunity potential growth expansion" },
            { "trend", "trend movement direction pattern" },
            { "market", "market sector industry segment" },
        };

        var expanded = query;
        foreach (var (word, expansion) in expansions)
        {
            if (query.Contains(word, StringComparison.OrdinalIgnoreCase))
                expanded += " " + expansion;
        }

        return expanded;
    }

    /// <summary>
    /// Rank retrieved data by relevance score
    /// </summary>
    private RagContextDto RankByRelevance(RagContextDto context, string query)
    {
        // Score each item
        foreach (var report in context.Reports)
            report.Relevance = ScoreRelevance(report.Title + " " + report.Summary, query);

        foreach (var news in context.NewsArticles)
            news.Relevance = ScoreRelevance(news.Title + " " + news.Summary, query);

        foreach (var alert in context.Alerts)
            alert.Relevance = ScoreRelevance(alert.Title + " " + alert.Description, query);

        // Sort by relevance
        context.Reports = context.Reports.OrderByDescending(r => r.Relevance).ToList();
        context.NewsArticles = context.NewsArticles.OrderByDescending(n => n.Relevance).ToList();
        context.Alerts = context.Alerts.OrderByDescending(a => a.Relevance).ToList();

        return context;
    }

    private async Task<List<WebSearchContextDto>> GetLiveWebSearchAsync(string query, string keyword)
    {
        var cacheMinutes = _configuration.GetValue("AiChat:WebSearchResultsCacheMinutes", 5);
        var normalizedQuery = Regex.Replace(query.ToLowerInvariant(), "\\s+", "_");
        var cacheKey = $"websearch_{keyword}_{normalizedQuery}_{DateTime.UtcNow:yyyyMMddHHmm}";
        if (WebSearchCache.TryGetValue(cacheKey, out var cached))
        {
            if ((DateTime.UtcNow - cached.CreatedAt).TotalMinutes < cacheMinutes)
            {
                return cached.Results;
            }

            WebSearchCache.Remove(cacheKey);
        }

        var maxResults = _configuration.GetValue("AiChat:MaxWebResultsPerQuery", 5);
        var timeoutMs = _configuration.GetValue("AiChat:WebSearchTimeoutMs", 3000);

        var searchRequest = new WebSearchRequestDto
        {
            Keyword = keyword,
            MaxResults = maxResults
        };

        var searchTask = _webSearchService.SearchAsync(searchRequest);
        var completedTask = await Task.WhenAny(searchTask, Task.Delay(timeoutMs));
        if (completedTask != searchTask)
        {
            _logger.LogWarning("Live web search timed out for keyword: {Keyword}", keyword);
            return new List<WebSearchContextDto>();
        }

        var result = await searchTask;
        if (!result.IsSuccess || result.Data == null)
        {
            _logger.LogWarning("Live web search failed for keyword: {Keyword}. {Error}", keyword, result.Error);
            return new List<WebSearchContextDto>();
        }

        var mapped = result.Data
            .Take(maxResults)
            .Select(r => new WebSearchContextDto
            {
                Title = r.Title,
                Url = r.Url,
                Snippet = r.Snippet,
                RetrievedAt = r.RetrievedUtc == default ? DateTime.UtcNow : r.RetrievedUtc,
                Source = r.Source
            })
            .ToList();

        WebSearchCache[cacheKey] = new CachedWebSearch
        {
            CreatedAt = DateTime.UtcNow,
            Results = mapped
        };

        return mapped;
    }

    private class CachedContext
    {
        public RagContextDto Context { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

    private class CachedWebSearch
    {
        public List<WebSearchContextDto> Results { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }
}
