using Alfanar.MarketIntel.Application.Common;
using Alfanar.MarketIntel.Application.DTOs;
using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Domain.Entities;
using Alfanar.MarketIntel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Alfanar.MarketIntel.Application.Services;

public class CompetitorTrackingService : ICompetitorTrackingService
{
    private readonly ICompetitorRepository _competitorRepository;
    private readonly ICompetitorMentionRepository _mentionRepository;
    private readonly IDocumentAnalyzer _documentAnalyzer;
    private readonly MarketIntelDbContext _context;
    private readonly ILogger<CompetitorTrackingService> _logger;
    private readonly IConfiguration _configuration;

    public CompetitorTrackingService(
        ICompetitorRepository competitorRepository,
        ICompetitorMentionRepository mentionRepository,
        IDocumentAnalyzer documentAnalyzer,
        MarketIntelDbContext context,
        IConfiguration configuration,
        ILogger<CompetitorTrackingService> logger)
    {
        _competitorRepository = competitorRepository;
        _mentionRepository = mentionRepository;
        _documentAnalyzer = documentAnalyzer;
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Result<CompetitorDto>> AddCompetitorAsync(CreateCompetitorDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return Result<CompetitorDto>.Failure("Competitor name is required");

            var existing = await _competitorRepository.GetByNameAsync(dto.Name.Trim());
            if (existing != null)
                return Result<CompetitorDto>.Failure("Competitor already exists");

            var entity = new Competitor
            {
                Name = dto.Name.Trim(),
                Industry = dto.Industry.Trim(),
                Region = dto.Region.Trim(),
                Keywords = SerializeKeywords(dto.Keywords),
                Website = dto.Website?.Trim(),
                IsActive = dto.IsActive,
                IsAutoDetected = false,
                CreatedBy = "User",
                Notes = dto.Notes
            };

            await _competitorRepository.AddAsync(entity);
            await _competitorRepository.SaveChangesAsync();

            return Result<CompetitorDto>.Success(MapCompetitor(entity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding competitor {Name}", dto.Name);
            return Result<CompetitorDto>.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<Result<CompetitorDto>> UpdateCompetitorAsync(Guid id, CreateCompetitorDto dto)
    {
        try
        {
            var entity = await _competitorRepository.GetByIdAsync(id);
            if (entity == null)
                return Result<CompetitorDto>.Failure("Competitor not found");

            if (!string.Equals(entity.Name, dto.Name, StringComparison.OrdinalIgnoreCase))
            {
                var existing = await _competitorRepository.GetByNameAsync(dto.Name.Trim());
                if (existing != null && existing.Id != id)
                    return Result<CompetitorDto>.Failure("Competitor name already exists");
            }

            entity.Name = dto.Name.Trim();
            entity.Industry = dto.Industry.Trim();
            entity.Region = dto.Region.Trim();
            entity.Keywords = SerializeKeywords(dto.Keywords);
            entity.Website = dto.Website?.Trim();
            entity.IsActive = dto.IsActive;
            entity.Notes = dto.Notes;

            await _competitorRepository.UpdateAsync(entity);
            await _competitorRepository.SaveChangesAsync();

            return Result<CompetitorDto>.Success(MapCompetitor(entity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating competitor {Id}", id);
            return Result<CompetitorDto>.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<Result<bool>> DeleteCompetitorAsync(Guid id)
    {
        try
        {
            var entity = await _competitorRepository.GetByIdAsync(id);
            if (entity == null)
                return Result<bool>.Failure("Competitor not found");

            await _competitorRepository.DeleteAsync(entity);
            await _competitorRepository.SaveChangesAsync();

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting competitor {Id}", id);
            return Result<bool>.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<Result<List<CompetitorDto>>> GetCompetitorsAsync(bool includeInactive = true)
    {
        try
        {
            var competitors = await _competitorRepository.GetAllAsync(includeInactive);
            return Result<List<CompetitorDto>>.Success(competitors.Select(MapCompetitor).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving competitors");
            return Result<List<CompetitorDto>>.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<Result<List<CompetitorDto>>> GetAutoDetectedCompetitorsAsync()
    {
        try
        {
            var competitors = await _competitorRepository.GetAutoDetectedAsync();
            return Result<List<CompetitorDto>>.Success(competitors.Select(MapCompetitor).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving auto-detected competitors");
            return Result<List<CompetitorDto>>.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<Result<CompetitorDashboardDto>> GetCompetitorDashboardAsync(Guid id)
    {
        try
        {
            var competitor = await _competitorRepository.GetByIdAsync(id);
            if (competitor == null)
                return Result<CompetitorDashboardDto>.Failure("Competitor not found");

            var mentions = await _context.CompetitorMentions
                .Where(m => m.CompetitorId == id)
                .OrderByDescending(m => m.DetectedUtc)
                .ToListAsync();

            var last30Days = mentions.Where(m => m.DetectedUtc >= DateTime.UtcNow.AddDays(-30)).ToList();
            var avgSentiment = mentions.Count == 0
                ? 0
                : mentions.Where(m => m.SentimentScore.HasValue).Select(m => m.SentimentScore!.Value).DefaultIfEmpty(0).Average();

            var trend = mentions
                .GroupBy(m => StartOfWeek(m.DetectedUtc))
                .OrderBy(g => g.Key)
                .Select(g => new CompetitorMentionTrendPointDto
                {
                    WeekStart = g.Key,
                    Count = g.Count()
                })
                .ToList();

            var topContexts = mentions
                .GroupBy(m => string.IsNullOrWhiteSpace(m.MentionContext) ? "General" : m.MentionContext)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key)
                .ToList();

            var recentMentions = mentions
                .Take(10)
                .Select(m => MapMention(m, competitor.Name))
                .ToList();

            var dashboard = new CompetitorDashboardDto
            {
                Competitor = MapCompetitor(competitor),
                TotalMentions = mentions.Count,
                Last30DaysMentions = last30Days.Count,
                AverageSentiment = avgSentiment,
                TopContextTypes = topContexts,
                MentionTrend = trend,
                RecentMentions = recentMentions
            };

            return Result<CompetitorDashboardDto>.Success(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building competitor dashboard {Id}", id);
            return Result<CompetitorDashboardDto>.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<Result<CompetitorComparisonDto>> CompareCompetitorsAsync(List<Guid> ids)
    {
        try
        {
            var competitors = await _context.Competitors
                .Where(c => ids.Contains(c.Id))
                .ToListAsync();

            var mentions = await _context.CompetitorMentions
                .Where(m => ids.Contains(m.CompetitorId))
                .ToListAsync();

            var items = competitors.Select(c =>
            {
                var competitorMentions = mentions.Where(m => m.CompetitorId == c.Id).ToList();
                var last30 = competitorMentions.Count(m => m.DetectedUtc >= DateTime.UtcNow.AddDays(-30));
                var avgSentiment = competitorMentions.Count == 0
                    ? 0
                    : competitorMentions.Where(m => m.SentimentScore.HasValue).Select(m => m.SentimentScore!.Value).DefaultIfEmpty(0).Average();

                return new CompetitorComparisonItemDto
                {
                    CompetitorId = c.Id,
                    Name = c.Name,
                    TotalMentions = competitorMentions.Count,
                    Last30DaysMentions = last30,
                    AverageSentiment = avgSentiment
                };
            }).ToList();

            return Result<CompetitorComparisonDto>.Success(new CompetitorComparisonDto { Items = items });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing competitors");
            return Result<CompetitorComparisonDto>.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<Result<List<CompetitorMentionDto>>> ScanForMentionsAsync(Guid competitorId)
    {
        try
        {
            var competitor = await _competitorRepository.GetByIdAsync(competitorId);
            if (competitor == null)
                return Result<List<CompetitorMentionDto>>.Failure("Competitor not found");

            var keywords = DeserializeKeywords(competitor.Keywords);
            var searchTerms = new List<string> { competitor.Name };
            searchTerms.AddRange(keywords);

            var newsMatches = await _context.NewsArticles
                .Where(n => searchTerms.Any(t => n.Title.Contains(t) || n.Summary.Contains(t) || n.BodyText.Contains(t)))
                .OrderByDescending(n => n.PublishedUtc)
                .Take(200)
                .ToListAsync();

            var webMatches = await _context.WebSearchResults
                .Where(w => searchTerms.Any(t => w.Title.Contains(t) || w.Snippet.Contains(t)))
                .OrderByDescending(w => w.RetrievedUtc)
                .Take(200)
                .ToListAsync();

            var mentions = new List<CompetitorMention>();

            foreach (var article in newsMatches)
            {
                if (await _mentionRepository.ExistsAsync(competitorId, "News", article.Id))
                    continue;

                mentions.Add(new CompetitorMention
                {
                    CompetitorId = competitorId,
                    SourceType = "News",
                    SourceId = article.Id,
                    Title = article.Title,
                    Snippet = article.Summary,
                    Url = article.Url,
                    MentionContext = "General",
                    DetectedUtc = DateTime.UtcNow,
                    IsAutoDetected = true
                });
            }

            foreach (var result in webMatches)
            {
                if (await _mentionRepository.ExistsAsync(competitorId, "WebSearch", result.Id))
                    continue;

                mentions.Add(new CompetitorMention
                {
                    CompetitorId = competitorId,
                    SourceType = "WebSearch",
                    SourceId = result.Id,
                    Title = result.Title,
                    Snippet = result.Snippet,
                    Url = result.Url,
                    MentionContext = "General",
                    DetectedUtc = DateTime.UtcNow,
                    IsAutoDetected = true
                });
            }

            if (mentions.Count > 0)
            {
                await _mentionRepository.AddRangeAsync(mentions);
                await _mentionRepository.SaveChangesAsync();
            }

            var dtos = mentions.Select(m => MapMention(m, competitor.Name)).ToList();
            return Result<List<CompetitorMentionDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning mentions for competitor {Id}", competitorId);
            return Result<List<CompetitorMentionDto>>.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<Result<CompetitorDetectionResultDto>> AutoDetectCompetitorsAsync(string articleText)
    {
        try
        {
            var known = await _competitorRepository.GetAllAsync(false);
            var knownNames = known.Select(c => c.Name).ToList();

            var result = await _documentAnalyzer.ExtractCompetitorMentionsAsync(articleText, knownNames);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-detecting competitors");
            return Result<CompetitorDetectionResultDto>.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<Result<List<CompetitorMentionDto>>> ScanArticleAsync(CompetitorScanRequestDto request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                return Result<List<CompetitorMentionDto>>.Failure("Title is required");

            var enableDetection = _configuration.GetValue("CompetitorTracking:AutoDetect", true);
            if (!enableDetection)
                return Result<List<CompetitorMentionDto>>.Success(new List<CompetitorMentionDto>());

            var text = string.Join("\n", new[]
            {
                request.Title,
                request.Snippet ?? string.Empty,
                request.BodyText ?? string.Empty
            });

            var competitors = await _competitorRepository.GetAllAsync(true);
            var knownNames = competitors.Where(c => c.IsActive).Select(c => c.Name).ToList();

            CompetitorDetectionResultDto? detectionData = null;
            var detection = await _documentAnalyzer.ExtractCompetitorMentionsAsync(text, knownNames);
            if (detection.IsSuccess)
            {
                detectionData = detection.Data;
            }
            else
            {
                _logger.LogWarning("Competitor AI detection failed, falling back to keyword matching: {Error}", detection.Error);
            }

            var mentions = new List<CompetitorMention>();

            if (detectionData != null)
            {
                foreach (var mention in detectionData.Mentions)
                {
                    var competitor = competitors.FirstOrDefault(c =>
                        string.Equals(c.Name, mention.Name, StringComparison.OrdinalIgnoreCase));

                    if (competitor == null)
                        continue;

                    if (await _mentionRepository.ExistsAsync(competitor.Id, request.SourceType, request.SourceId))
                        continue;

                    mentions.Add(new CompetitorMention
                    {
                        CompetitorId = competitor.Id,
                        SourceType = request.SourceType,
                        SourceId = request.SourceId,
                        Title = request.Title,
                        Snippet = request.Snippet ?? string.Empty,
                        Url = request.Url ?? string.Empty,
                        MentionContext = mention.Context ?? "General",
                        SentimentScore = mention.SentimentScore,
                        SentimentLabel = mention.SentimentLabel,
                        DetectedUtc = DateTime.UtcNow,
                        IsAutoDetected = true
                    });
                }

                foreach (var newCompetitor in detectionData.NewCompetitors)
                {
                    var existing = await _competitorRepository.GetByNameAsync(newCompetitor.Name);
                    if (existing != null)
                        continue;

                    var suggested = new Competitor
                    {
                        Name = newCompetitor.Name,
                        Industry = newCompetitor.Industry ?? string.Empty,
                        Region = string.Empty,
                        Keywords = SerializeKeywords(new List<string>()),
                        IsActive = false,
                        IsAutoDetected = true,
                        CreatedBy = "AutoDetected",
                        Notes = newCompetitor.Reason
                    };

                    await _competitorRepository.AddAsync(suggested);
                }
            }

            if (mentions.Count == 0)
            {
                foreach (var competitor in competitors.Where(c => c.IsActive))
                {
                    if (await _mentionRepository.ExistsAsync(competitor.Id, request.SourceType, request.SourceId))
                        continue;

                    if (!MatchesCompetitor(text, competitor))
                        continue;

                    mentions.Add(new CompetitorMention
                    {
                        CompetitorId = competitor.Id,
                        SourceType = request.SourceType,
                        SourceId = request.SourceId,
                        Title = request.Title,
                        Snippet = request.Snippet ?? string.Empty,
                        Url = request.Url ?? string.Empty,
                        MentionContext = "KeywordMatch",
                        DetectedUtc = DateTime.UtcNow,
                        IsAutoDetected = true
                    });
                }
            }

            if (mentions.Count > 0)
            {
                await _mentionRepository.AddRangeAsync(mentions);
            }

            await _competitorRepository.SaveChangesAsync();
            await _mentionRepository.SaveChangesAsync();

            var dtos = mentions.Select(m => MapMention(m, competitors.FirstOrDefault(c => c.Id == m.CompetitorId)?.Name ?? string.Empty)).ToList();
            return Result<List<CompetitorMentionDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning article for competitor mentions");
            return Result<List<CompetitorMentionDto>>.Failure($"Error: {ex.Message}");
        }
    }

    private static string SerializeKeywords(List<string> keywords)
    {
        return keywords == null || keywords.Count == 0
            ? string.Empty
            : System.Text.Json.JsonSerializer.Serialize(keywords);
    }

    private static List<string> DeserializeKeywords(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new List<string>();

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(raw) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static DateTime StartOfWeek(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.Date.AddDays(-diff);
    }

    private static CompetitorDto MapCompetitor(Competitor competitor)
    {
        return new CompetitorDto
        {
            Id = competitor.Id,
            Name = competitor.Name,
            Industry = competitor.Industry,
            Region = competitor.Region,
            Keywords = DeserializeKeywords(competitor.Keywords),
            Website = competitor.Website,
            IsActive = competitor.IsActive,
            IsAutoDetected = competitor.IsAutoDetected,
            CreatedUtc = competitor.CreatedUtc,
            CreatedBy = competitor.CreatedBy,
            Notes = competitor.Notes
        };
    }

    private static CompetitorMentionDto MapMention(CompetitorMention mention, string competitorName)
    {
        return new CompetitorMentionDto
        {
            Id = mention.Id,
            CompetitorId = mention.CompetitorId,
            CompetitorName = competitorName,
            SourceType = mention.SourceType,
            SourceId = mention.SourceId,
            Title = mention.Title,
            Snippet = mention.Snippet,
            Url = mention.Url,
            SentimentScore = mention.SentimentScore,
            SentimentLabel = mention.SentimentLabel,
            MentionContext = mention.MentionContext,
            DetectedUtc = mention.DetectedUtc,
            IsAutoDetected = mention.IsAutoDetected
        };
    }

    private static bool MatchesCompetitor(string text, Competitor competitor)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        if (text.IndexOf(competitor.Name, StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        var keywords = DeserializeKeywords(competitor.Keywords);
        return keywords.Any(k => !string.IsNullOrWhiteSpace(k) &&
                                 text.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);
    }
}
