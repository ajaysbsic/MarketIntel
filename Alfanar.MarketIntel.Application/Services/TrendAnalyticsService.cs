using Alfanar.MarketIntel.Application.Common;
using Alfanar.MarketIntel.Application.DTOs;
using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Domain.Entities;
using Alfanar.MarketIntel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Alfanar.MarketIntel.Application.Services;

public class TrendAnalyticsService : ITrendAnalyticsService
{
    private readonly MarketIntelDbContext _context;
    private readonly ITrendSnapshotRepository _snapshotRepository;
    private readonly IDocumentAnalyzer _documentAnalyzer;
    private readonly ILogger<TrendAnalyticsService> _logger;

    public TrendAnalyticsService(
        MarketIntelDbContext context,
        ITrendSnapshotRepository snapshotRepository,
        IDocumentAnalyzer documentAnalyzer,
        ILogger<TrendAnalyticsService> logger)
    {
        _context = context;
        _snapshotRepository = snapshotRepository;
        _documentAnalyzer = documentAnalyzer;
        _logger = logger;
    }

    public async Task<Result<bool>> GenerateDailySnapshotAsync(DateTime date)
    {
        try
        {
            var dayStart = date.Date;
            var dayEnd = dayStart.AddDays(1);

            var keywords = await _context.KeywordMonitors
                .Where(k => k.IsActive)
                .Select(k => k.Keyword)
                .Distinct()
                .ToListAsync();

            foreach (var keyword in keywords)
            {
                var existing = await _snapshotRepository.GetByKeywordAndDateAsync(keyword, dayStart);
                if (existing != null)
                    continue;

                var newsCount = await _context.NewsArticles
                    .CountAsync(n => n.PublishedUtc >= dayStart && n.PublishedUtc < dayEnd &&
                                     (n.Title.Contains(keyword) || n.Summary.Contains(keyword)));

                var webCount = await _context.WebSearchResults
                    .CountAsync(w => w.RetrievedUtc >= dayStart && w.RetrievedUtc < dayEnd && w.Keyword == keyword);

                var competitorMentions = await _context.CompetitorMentions
                    .Where(m => m.DetectedUtc >= dayStart && m.DetectedUtc < dayEnd)
                    .ToListAsync();

                var competitorCounts = competitorMentions
                    .GroupBy(m => m.CompetitorId)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count());

                var topSources = await _context.WebSearchResults
                    .Where(w => w.RetrievedUtc >= dayStart && w.RetrievedUtc < dayEnd && w.Keyword == keyword)
                    .GroupBy(w => w.Source)
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .Select(g => g.Key)
                    .ToListAsync();

                var avgSentiment = competitorMentions
                    .Where(m => m.SentimentScore.HasValue)
                    .Select(m => m.SentimentScore!.Value)
                    .DefaultIfEmpty(0)
                    .Average();

                var mentionCount = newsCount + webCount + competitorMentions.Count;
                var signalStrength = Math.Min(100, (newsCount + webCount) * 5);

                var snapshot = new TrendSnapshot
                {
                    Keyword = keyword,
                    SnapshotDate = dayStart,
                    MentionCount = mentionCount,
                    NewsCount = newsCount,
                    WebSearchCount = webCount,
                    AverageSentiment = avgSentiment,
                    TopSources = System.Text.Json.JsonSerializer.Serialize(topSources),
                    CompetitorMentionCounts = System.Text.Json.JsonSerializer.Serialize(competitorCounts),
                    SignalStrength = signalStrength,
                    CreatedUtc = DateTime.UtcNow
                };

                await _snapshotRepository.AddAsync(snapshot);
            }

            await _snapshotRepository.SaveChangesAsync();
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating daily trend snapshot");
            return Result<bool>.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<Result<List<TrendPointDto>>> GetKeywordTrendAsync(string keyword, int days)
    {
        try
        {
            var fromDate = DateTime.UtcNow.Date.AddDays(-Math.Abs(days));
            var toDate = DateTime.UtcNow.Date;

            var snapshots = await _snapshotRepository.GetByKeywordRangeAsync(keyword, fromDate, toDate);

            var points = snapshots.Select(s => new TrendPointDto
            {
                Date = s.SnapshotDate,
                Count = s.MentionCount,
                Sentiment = s.AverageSentiment
            }).ToList();

            return Result<List<TrendPointDto>>.Success(points);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving keyword trend for {Keyword}", keyword);
            return Result<List<TrendPointDto>>.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<Result<List<CompetitorVisibilityPointDto>>> GetCompetitorVisibilityAsync(Guid competitorId, int days)
    {
        try
        {
            var fromDate = DateTime.UtcNow.Date.AddDays(-Math.Abs(days));
            var mentions = await _context.CompetitorMentions
                .Where(m => m.CompetitorId == competitorId && m.DetectedUtc >= fromDate)
                .ToListAsync();

            var points = mentions
                .GroupBy(m => m.DetectedUtc.Date)
                .OrderBy(g => g.Key)
                .Select(g => new CompetitorVisibilityPointDto
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .ToList();

            return Result<List<CompetitorVisibilityPointDto>>.Success(points);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving competitor visibility for {Id}", competitorId);
            return Result<List<CompetitorVisibilityPointDto>>.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<Result<List<NoiseSignalPointDto>>> GetMarketNoiseVsSignalAsync(string keyword, int days)
    {
        try
        {
            var fromDate = DateTime.UtcNow.Date.AddDays(-Math.Abs(days));
            var toDate = DateTime.UtcNow.Date;
            var snapshots = await _snapshotRepository.GetByKeywordRangeAsync(keyword, fromDate, toDate);

            var points = snapshots.Select(s => new NoiseSignalPointDto
            {
                Date = s.SnapshotDate,
                NoiseCount = s.WebSearchCount + s.NewsCount,
                SignalCount = Math.Min(s.MentionCount, s.SignalStrength / 5)
            }).ToList();

            return Result<List<NoiseSignalPointDto>>.Success(points);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving noise vs signal for {Keyword}", keyword);
            return Result<List<NoiseSignalPointDto>>.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<Result<TrendComparisonDto>> GetTrendComparisonAsync(List<string> keywords, int days)
    {
        try
        {
            var fromDate = DateTime.UtcNow.Date.AddDays(-Math.Abs(days));
            var toDate = DateTime.UtcNow.Date;
            var series = new List<TrendSeriesDto>();

            foreach (var keyword in keywords)
            {
                var snapshots = await _snapshotRepository.GetByKeywordRangeAsync(keyword, fromDate, toDate);
                var points = snapshots.Select(s => new TrendPointDto
                {
                    Date = s.SnapshotDate,
                    Count = s.MentionCount,
                    Sentiment = s.AverageSentiment
                }).ToList();

                series.Add(new TrendSeriesDto
                {
                    Keyword = keyword,
                    Points = points
                });
            }

            return Result<TrendComparisonDto>.Success(new TrendComparisonDto { Series = series });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing trends");
            return Result<TrendComparisonDto>.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<Result<WeeklyDigestDto>> GetWeeklyDigestAsync()
    {
        try
        {
            var fromDate = DateTime.UtcNow.Date.AddDays(-7);
            var snapshots = await _context.TrendSnapshots
                .Where(s => s.SnapshotDate >= fromDate)
                .OrderBy(s => s.SnapshotDate)
                .ToListAsync();

            if (snapshots.Count == 0)
            {
                return Result<WeeklyDigestDto>.Success(new WeeklyDigestDto
                {
                    Summary = "No trend snapshots available for the last 7 days. Generate snapshots or allow the background job to run.",
                    GeneratedUtc = DateTime.UtcNow
                });
            }

            var summaryText = string.Join("\n", snapshots.Select(s =>
                $"{s.SnapshotDate:yyyy-MM-dd} {s.Keyword}: mentions={s.MentionCount}, news={s.NewsCount}, web={s.WebSearchCount}, signal={s.SignalStrength}"));

            string digest;
            if (_documentAnalyzer.IsAvailable())
            {
                var summary = await _documentAnalyzer.GenerateSummaryAsync(summaryText, 160);
                digest = summary.IsSuccess ? summary.Data ?? "" : "";
            }
            else
            {
                digest = "Weekly trend digest is unavailable (AI not configured).";
            }

            if (string.IsNullOrWhiteSpace(digest))
            {
                digest = "Weekly trend digest is unavailable. There is not enough data to summarize yet.";
            }

            return Result<WeeklyDigestDto>.Success(new WeeklyDigestDto
            {
                Summary = digest,
                GeneratedUtc = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating weekly digest");
            return Result<WeeklyDigestDto>.Failure($"Error: {ex.Message}");
        }
    }
}
