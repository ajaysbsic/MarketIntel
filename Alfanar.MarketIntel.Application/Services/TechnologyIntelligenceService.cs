using Alfanar.MarketIntel.Application.Common;
using Alfanar.MarketIntel.Application.DTOs;
using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Domain.Entities;
using Alfanar.MarketIntel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Alfanar.MarketIntel.Application.Services;

public class TechnologyIntelligenceService : ITechnologyIntelligenceService
{
    private readonly MarketIntelDbContext _context;
    private readonly ILogger<TechnologyIntelligenceService> _logger;

    public TechnologyIntelligenceService(MarketIntelDbContext context, ILogger<TechnologyIntelligenceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<TechnologyOverviewDto>> GetOverviewAsync(TechnologyIntelligenceFilterDto filter)
    {
        try
        {
            var (newsQuery, reportQuery) = BuildQueries(filter);

            var newsCount = newsQuery != null ? await newsQuery.CountAsync() : 0;
            var reportCount = reportQuery != null ? await reportQuery.CountAsync() : 0;

            var regionCount = await GetDistinctRegionCountAsync(newsQuery, reportQuery);
            var topKeywords = await GetTopKeywordsAsync(newsQuery, reportQuery, filter.Keywords);

            var overview = new TechnologyOverviewDto
            {
                NewsCount = newsCount,
                ReportCount = reportCount,
                TotalItems = newsCount + reportCount,
                DistinctRegions = regionCount,
                TopKeywords = topKeywords
            };

            return Result<TechnologyOverviewDto>.Success(overview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building technology overview");
            return Result<TechnologyOverviewDto>.Failure($"Failed to build overview: {ex.Message}");
        }
    }

    public async Task<Result<List<TechnologyTrendPointDto>>> GetTimelineAsync(TechnologyIntelligenceFilterDto filter)
    {
        try
        {
            var (newsQuery, reportQuery) = BuildQueries(filter);
            var timeline = await BuildTimelineAsync(newsQuery, reportQuery);
            return Result<List<TechnologyTrendPointDto>>.Success(timeline);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building technology timeline");
            return Result<List<TechnologyTrendPointDto>>.Failure($"Failed to build timeline: {ex.Message}");
        }
    }

    public async Task<Result<List<TechnologyRegionSignalDto>>> GetRegionsAsync(TechnologyIntelligenceFilterDto filter)
    {
        try
        {
            var (newsQuery, reportQuery) = BuildQueries(filter);
            var regions = await BuildRegionsAsync(newsQuery, reportQuery);
            return Result<List<TechnologyRegionSignalDto>>.Success(regions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building region signals");
            return Result<List<TechnologyRegionSignalDto>>.Failure($"Failed to build regions: {ex.Message}");
        }
    }

    public async Task<Result<List<TechnologyKeyPlayerDto>>> GetKeyPlayersAsync(TechnologyIntelligenceFilterDto filter, int maxItems = 10)
    {
        try
        {
            var (newsQuery, reportQuery) = BuildQueries(filter);
            var keyPlayers = await BuildKeyPlayersAsync(newsQuery, reportQuery, maxItems);
            return Result<List<TechnologyKeyPlayerDto>>.Success(keyPlayers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building key players");
            return Result<List<TechnologyKeyPlayerDto>>.Failure($"Failed to build key players: {ex.Message}");
        }
    }

    public async Task<Result<List<TechnologyInsightDto>>> GetInsightsAsync(TechnologyIntelligenceFilterDto filter)
    {
        try
        {
            var summary = await GetSummaryInternalAsync(filter);
            if (!summary.IsSuccess || summary.Data == null)
            {
                return Result<List<TechnologyInsightDto>>.Failure(summary.Error ?? "Failed to build insights");
            }

            return Result<List<TechnologyInsightDto>>.Success(summary.Data.Insights);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building insights");
            return Result<List<TechnologyInsightDto>>.Failure($"Failed to build insights: {ex.Message}");
        }
    }

    public async Task<Result<TechnologyIntelligenceSummaryDto>> GetSummaryAsync(TechnologyIntelligenceFilterDto filter)
    {
        try
        {
            var summary = await GetSummaryInternalAsync(filter);
            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building technology summary");
            return Result<TechnologyIntelligenceSummaryDto>.Failure($"Failed to build summary: {ex.Message}");
        }
    }

    private async Task<Result<TechnologyIntelligenceSummaryDto>> GetSummaryInternalAsync(TechnologyIntelligenceFilterDto filter)
    {
        var overviewResult = await GetOverviewAsync(filter);
        if (!overviewResult.IsSuccess || overviewResult.Data == null)
        {
            return Result<TechnologyIntelligenceSummaryDto>.Failure(overviewResult.Error ?? "Overview failed");
        }

        var timelineResult = await GetTimelineAsync(filter);
        var regionsResult = await GetRegionsAsync(filter);
        var keyPlayersResult = await GetKeyPlayersAsync(filter, 10);

        var insights = BuildInsights(
            overviewResult.Data,
            timelineResult.Data ?? new List<TechnologyTrendPointDto>(),
            regionsResult.Data ?? new List<TechnologyRegionSignalDto>(),
            keyPlayersResult.Data ?? new List<TechnologyKeyPlayerDto>());

        return Result<TechnologyIntelligenceSummaryDto>.Success(new TechnologyIntelligenceSummaryDto
        {
            Overview = overviewResult.Data,
            Timeline = timelineResult.Data ?? new List<TechnologyTrendPointDto>(),
            Regions = regionsResult.Data ?? new List<TechnologyRegionSignalDto>(),
            KeyPlayers = keyPlayersResult.Data ?? new List<TechnologyKeyPlayerDto>(),
            Insights = insights
        });
    }

    private (IQueryable<NewsArticle>? newsQuery, IQueryable<FinancialReport>? reportQuery) BuildQueries(TechnologyIntelligenceFilterDto filter)
    {
        var includeNews = IncludeSource(filter, "news");
        var includeReports = IncludeSource(filter, "reports");

        IQueryable<NewsArticle>? newsQuery = null;
        IQueryable<FinancialReport>? reportQuery = null;

        if (includeNews)
        {
            newsQuery = _context.NewsArticles
                .Include(n => n.NewsArticleTags)
                .ThenInclude(nat => nat.Tag)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Region))
                newsQuery = newsQuery.Where(n => n.Region == filter.Region);

            if (filter.FromDate.HasValue)
                newsQuery = newsQuery.Where(n => n.PublishedUtc >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                newsQuery = newsQuery.Where(n => n.PublishedUtc <= filter.ToDate.Value);

            if (filter.Keywords != null && filter.Keywords.Any())
            {
                var normalized = NormalizeKeywords(filter.Keywords);
                newsQuery = newsQuery.Where(n => n.NewsArticleTags
                    .Any(t => normalized.Contains(t.Tag.NormalizedName)));
            }
        }

        if (includeReports)
        {
            reportQuery = _context.FinancialReports
                .Include(r => r.FinancialReportTags)
                .ThenInclude(rt => rt.Tag)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Region))
                reportQuery = reportQuery.Where(r => r.Region == filter.Region);

            if (filter.FromDate.HasValue)
                reportQuery = reportQuery.Where(r => (r.PublishedDate ?? r.CreatedUtc) >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                reportQuery = reportQuery.Where(r => (r.PublishedDate ?? r.CreatedUtc) <= filter.ToDate.Value);

            if (filter.Keywords != null && filter.Keywords.Any())
            {
                var normalized = NormalizeKeywords(filter.Keywords);
                reportQuery = reportQuery.Where(r => r.FinancialReportTags
                    .Any(t => normalized.Contains(t.Tag.NormalizedName)));
            }
        }

        return (newsQuery, reportQuery);
    }

    private static bool IncludeSource(TechnologyIntelligenceFilterDto filter, string sourceType)
    {
        if (filter.SourceTypes == null || filter.SourceTypes.Count == 0)
            return true;

        return filter.SourceTypes.Any(s => string.Equals(s, sourceType, StringComparison.OrdinalIgnoreCase));
    }

    private static HashSet<string> NormalizeKeywords(IEnumerable<string> keywords)
    {
        return keywords
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim().ToUpperInvariant())
            .ToHashSet();
    }

    private static async Task<int> GetDistinctRegionCountAsync(
        IQueryable<NewsArticle>? newsQuery,
        IQueryable<FinancialReport>? reportQuery)
    {
        var regions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (newsQuery != null)
        {
            var newsRegions = await newsQuery
                .Select(n => n.Region)
                .Distinct()
                .ToListAsync();

            foreach (var region in newsRegions)
            {
                if (!string.IsNullOrWhiteSpace(region))
                    regions.Add(region);
            }
        }

        if (reportQuery != null)
        {
            var reportRegions = await reportQuery
                .Select(r => r.Region ?? "Global")
                .Distinct()
                .ToListAsync();

            foreach (var region in reportRegions)
            {
                if (!string.IsNullOrWhiteSpace(region))
                    regions.Add(region);
            }
        }

        return regions.Count;
    }

    private async Task<List<string>> GetTopKeywordsAsync(
        IQueryable<NewsArticle>? newsQuery,
        IQueryable<FinancialReport>? reportQuery,
        List<string>? filterKeywords)
    {
        if (filterKeywords != null && filterKeywords.Any())
        {
            return filterKeywords.Where(k => !string.IsNullOrWhiteSpace(k)).Distinct().ToList();
        }

        var keywordCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        if (newsQuery != null)
        {
            var newsTags = await newsQuery
                .SelectMany(n => n.NewsArticleTags.Select(t => t.Tag.Name))
                .ToListAsync();

            foreach (var tag in newsTags)
            {
                if (string.IsNullOrWhiteSpace(tag))
                    continue;

                keywordCounts[tag] = keywordCounts.TryGetValue(tag, out var count) ? count + 1 : 1;
            }
        }

        if (reportQuery != null)
        {
            var reportTags = await reportQuery
                .SelectMany(r => r.FinancialReportTags.Select(t => t.Tag.Name))
                .ToListAsync();

            foreach (var tag in reportTags)
            {
                if (string.IsNullOrWhiteSpace(tag))
                    continue;

                keywordCounts[tag] = keywordCounts.TryGetValue(tag, out var count) ? count + 1 : 1;
            }
        }

        return keywordCounts
            .OrderByDescending(k => k.Value)
            .Take(8)
            .Select(k => k.Key)
            .ToList();
    }

    private static async Task<List<TechnologyTrendPointDto>> BuildTimelineAsync(
        IQueryable<NewsArticle>? newsQuery,
        IQueryable<FinancialReport>? reportQuery)
    {
        var timeline = new Dictionary<DateTime, TechnologyTrendPointDto>();

        if (newsQuery != null)
        {
            var newsSeries = await newsQuery
                .GroupBy(n => new { n.PublishedUtc.Year, n.PublishedUtc.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Count = g.Count()
                })
                .ToListAsync();

            foreach (var point in newsSeries)
            {
                var period = new DateTime(point.Year, point.Month, 1);
                if (!timeline.TryGetValue(period, out var dto))
                {
                    dto = new TechnologyTrendPointDto { PeriodStart = period };
                    timeline[period] = dto;
                }
                dto.NewsCount = point.Count;
                dto.TotalCount = dto.NewsCount + dto.ReportCount;
            }
        }

        if (reportQuery != null)
        {
            var reportSeries = await reportQuery
                .GroupBy(r => new
                {
                    Year = (r.PublishedDate ?? r.CreatedUtc).Year,
                    Month = (r.PublishedDate ?? r.CreatedUtc).Month
                })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Count = g.Count()
                })
                .ToListAsync();

            foreach (var point in reportSeries)
            {
                var period = new DateTime(point.Year, point.Month, 1);
                if (!timeline.TryGetValue(period, out var dto))
                {
                    dto = new TechnologyTrendPointDto { PeriodStart = period };
                    timeline[period] = dto;
                }
                dto.ReportCount = point.Count;
                dto.TotalCount = dto.NewsCount + dto.ReportCount;
            }
        }

        return timeline
            .Values
            .OrderBy(p => p.PeriodStart)
            .ToList();
    }

    private static async Task<List<TechnologyRegionSignalDto>> BuildRegionsAsync(
        IQueryable<NewsArticle>? newsQuery,
        IQueryable<FinancialReport>? reportQuery)
    {
        var regions = new Dictionary<string, TechnologyRegionSignalDto>(StringComparer.OrdinalIgnoreCase);

        if (newsQuery != null)
        {
            var newsRegions = await newsQuery
                .GroupBy(n => n.Region ?? "Global")
                .Select(g => new { Region = g.Key, Count = g.Count() })
                .ToListAsync();

            foreach (var region in newsRegions)
            {
                var key = region.Region ?? "Global";
                if (!regions.TryGetValue(key, out var dto))
                {
                    dto = new TechnologyRegionSignalDto { Region = key };
                    regions[key] = dto;
                }
                dto.NewsCount = region.Count;
                dto.TotalCount = dto.NewsCount + dto.ReportCount;
            }
        }

        if (reportQuery != null)
        {
            var reportRegions = await reportQuery
                .GroupBy(r => r.Region ?? "Global")
                .Select(g => new { Region = g.Key, Count = g.Count() })
                .ToListAsync();

            foreach (var region in reportRegions)
            {
                var key = region.Region ?? "Global";
                if (!regions.TryGetValue(key, out var dto))
                {
                    dto = new TechnologyRegionSignalDto { Region = key };
                    regions[key] = dto;
                }
                dto.ReportCount = region.Count;
                dto.TotalCount = dto.NewsCount + dto.ReportCount;
            }
        }

        return regions
            .Values
            .OrderByDescending(r => r.TotalCount)
            .ToList();
    }

    private static async Task<List<TechnologyKeyPlayerDto>> BuildKeyPlayersAsync(
        IQueryable<NewsArticle>? newsQuery,
        IQueryable<FinancialReport>? reportQuery,
        int maxItems)
    {
        var results = new List<TechnologyKeyPlayerDto>();

        if (newsQuery != null)
        {
            var newsPlayers = await newsQuery
                .GroupBy(n => n.Source)
                .Select(g => new TechnologyKeyPlayerDto
                {
                    Name = g.Key,
                    SourceType = "news",
                    Mentions = g.Count()
                })
                .OrderByDescending(g => g.Mentions)
                .Take(maxItems)
                .ToListAsync();

            results.AddRange(newsPlayers);
        }

        if (reportQuery != null)
        {
            var reportPlayers = await reportQuery
                .GroupBy(r => r.CompanyName)
                .Select(g => new TechnologyKeyPlayerDto
                {
                    Name = g.Key,
                    SourceType = "reports",
                    Mentions = g.Count()
                })
                .OrderByDescending(g => g.Mentions)
                .Take(maxItems)
                .ToListAsync();

            results.AddRange(reportPlayers);
        }

        return results
            .OrderByDescending(r => r.Mentions)
            .Take(maxItems)
            .ToList();
    }

    private static List<TechnologyInsightDto> BuildInsights(
        TechnologyOverviewDto overview,
        List<TechnologyTrendPointDto> timeline,
        List<TechnologyRegionSignalDto> regions,
        List<TechnologyKeyPlayerDto> keyPlayers)
    {
        var insights = new List<TechnologyInsightDto>();

        if (regions.Any())
        {
            var topRegion = regions.OrderByDescending(r => r.TotalCount).First();
            if (overview.TotalItems > 0)
            {
                var share = (double)topRegion.TotalCount / overview.TotalItems;
                if (share >= 0.4)
                {
                    insights.Add(new TechnologyInsightDto
                    {
                        Title = $"Activity concentrated in {topRegion.Region}",
                        Detail = $"{topRegion.Region} accounts for {(share * 100):F0}% of tracked activity.",
                        InsightType = "regional"
                    });
                }
            }
        }

        if (timeline.Count >= 6)
        {
            var lastThree = timeline.TakeLast(3).Sum(t => t.TotalCount);
            var prevThree = timeline.Skip(Math.Max(0, timeline.Count - 6)).Take(3).Sum(t => t.TotalCount);

            if (prevThree > 0)
            {
                var change = (lastThree - prevThree) / (double)prevThree;
                var direction = change >= 0 ? "up" : "down";
                insights.Add(new TechnologyInsightDto
                {
                    Title = $"Momentum is {direction}",
                    Detail = $"Activity is {Math.Abs(change) * 100:F0}% {direction} versus the previous three periods.",
                    InsightType = "trend"
                });
            }
        }

        if (keyPlayers.Any())
        {
            var names = keyPlayers.Select(k => k.Name).Take(3).ToList();
            insights.Add(new TechnologyInsightDto
            {
                Title = "Top players emerging",
                Detail = $"Top mentions include {string.Join(", ", names)}.",
                InsightType = "players"
            });
        }

        if (insights.Count == 0)
        {
            insights.Add(new TechnologyInsightDto
            {
                Title = "Signals still forming",
                Detail = "Expand the date range or adjust keywords to surface clearer trends.",
                InsightType = "info"
            });
        }

        return insights;
    }
}
