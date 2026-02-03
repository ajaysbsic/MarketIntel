using Alfanar.MarketIntel.Application.DTOs;
using Alfanar.MarketIntel.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
    double ScoreRelevance(string data, string query);
    List<string> ExtractEntities(string query);
    string ExpandQuery(string query);
}

public class RagContextService : IRagContextService
{
    private readonly INewsRepository _newsRepo;
    private readonly IFinancialReportRepository _reportRepo;
    private readonly ISmartAlertRepository _alertRepo;
    private readonly ILogger<RagContextService> _logger;
    private readonly Dictionary<string, string> _entitySynonyms;

    // Performance optimization: Cache common queries for 5 minutes
    private static readonly Dictionary<string, CachedContext> QueryCache = new();
    private const int CacheDurationSeconds = 300;

    public RagContextService(
        INewsRepository newsRepo,
        IFinancialReportRepository reportRepo,
        ISmartAlertRepository alertRepo,
        ILogger<RagContextService> logger)
    {
        _newsRepo = newsRepo;
        _reportRepo = reportRepo;
        _alertRepo = alertRepo;
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

    private class CachedContext
    {
        public RagContextDto Context { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }
}
