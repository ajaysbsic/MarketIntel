using Alfanar.MarketIntel.Application.Common;
using Alfanar.MarketIntel.Application.DTOs;
using Alfanar.MarketIntel.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Alfanar.MarketIntel.Application.Services;

/// <summary>
/// Service for AI-curated intelligence from web search results
/// </summary>
public class ArticleCurationService : IArticleCurationService
{
    private const double DuplicateThreshold = 0.88;
    private const double ClusterThreshold = 0.82;

    private readonly IDocumentAnalyzer _documentAnalyzer;
    private readonly ILogger<ArticleCurationService> _logger;

    public ArticleCurationService(
        IDocumentAnalyzer documentAnalyzer,
        ILogger<ArticleCurationService> logger)
    {
        _documentAnalyzer = documentAnalyzer;
        _logger = logger;
    }

    public async Task<Result<CuratedIntelligenceDto>> CurateArticlesAsync(List<WebSearchResultDto> articles, string keyword)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return Result<CuratedIntelligenceDto>.Failure("Keyword is required");

            var originalCount = articles.Count;
            var uniqueArticles = DeduplicateArticles(articles);
            var clusters = ClusterArticles(uniqueArticles);

            var curatedItems = new List<CuratedItemDto>();
            foreach (var cluster in clusters)
            {
                var insight = await GenerateClusterInsightAsync(cluster, keyword);
                curatedItems.Add(new CuratedItemDto
                {
                    Title = cluster[0].Title,
                    KeyFact = insight.KeyFact ?? cluster[0].Title,
                    WhyItMatters = insight.WhyItMatters ?? $"Strategic relevance for {keyword} monitoring.",
                    Significance = Math.Clamp(insight.Significance, 1, 5),
                    SourceCount = cluster.Count,
                    Sources = cluster.Select(a => a.Url).Distinct().ToList(),
                    ClusterKeywords = ExtractClusterKeywords(cluster)
                });
            }

            curatedItems = curatedItems
                .OrderByDescending(item => item.Significance)
                .ThenByDescending(item => item.SourceCount)
                .ToList();

            var headlineInsight = BuildHeadlineInsight(curatedItems, keyword);
            var combinedSummary = await BuildCombinedSummaryAsync(uniqueArticles, keyword);

            return Result<CuratedIntelligenceDto>.Success(new CuratedIntelligenceDto
            {
                CombinedSummary = combinedSummary,
                HeadlineInsight = headlineInsight,
                CuratedItems = curatedItems,
                DeduplicationStats = new DeduplicationStatsDto
                {
                    OriginalCount = originalCount,
                    UniqueCount = uniqueArticles.Count,
                    DuplicatesRemoved = Math.Max(0, originalCount - uniqueArticles.Count)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error curating intelligence for keyword: {Keyword}", keyword);
            return Result<CuratedIntelligenceDto>.Failure($"Error: {ex.Message}");
        }
    }

    private async Task<string> BuildCombinedSummaryAsync(List<WebSearchResultDto> articles, string keyword)
    {
        if (articles.Count == 0)
            return $"No articles available to summarize for {keyword}.";

        var lines = articles
            .Take(30)
            .Select(a => $"Title: {TrimText(a.Title, 200)}\nSnippet: {TrimText(a.Snippet, 300)}")
            .ToList();

        var combinedText = string.Join("\n\n", lines);

        if (!_documentAnalyzer.IsAvailable())
            return BuildFallbackSummary(articles, keyword);

        var summary = await _documentAnalyzer.GenerateSummaryAsync(combinedText, 160);
        if (summary.IsSuccess && !string.IsNullOrWhiteSpace(summary.Data))
            return summary.Data;

        return BuildFallbackSummary(articles, keyword);
    }

    private static string BuildFallbackSummary(List<WebSearchResultDto> articles, string keyword)
    {
        var titles = articles
            .Select(a => a.Title)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Take(5)
            .ToList();

        if (titles.Count == 0)
            return $"Summary unavailable for {keyword}.";

        return $"Top stories for {keyword}: {string.Join(" | ", titles)}";
    }

    private static string TrimText(string? text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return text.Length <= maxLength ? text : text.Substring(0, maxLength) + "...";
    }

    private List<WebSearchResultDto> DeduplicateArticles(List<WebSearchResultDto> articles)
    {
        var unique = new List<WebSearchResultDto>();

        foreach (var article in articles)
        {
            var normalizedTitle = Normalize(article.Title);
            var isDuplicate = unique.Any(existing =>
                string.Equals(existing.Url, article.Url, StringComparison.OrdinalIgnoreCase) ||
                TitleSimilarity(normalizedTitle, Normalize(existing.Title)) >= DuplicateThreshold);

            if (!isDuplicate)
                unique.Add(article);
        }

        return unique;
    }

    private List<List<WebSearchResultDto>> ClusterArticles(List<WebSearchResultDto> articles)
    {
        var clusters = new List<List<WebSearchResultDto>>();

        foreach (var article in articles)
        {
            var normalizedTitle = Normalize(article.Title);
            var cluster = clusters.FirstOrDefault(c =>
                TitleSimilarity(normalizedTitle, Normalize(c[0].Title)) >= ClusterThreshold);

            if (cluster == null)
            {
                clusters.Add(new List<WebSearchResultDto> { article });
            }
            else
            {
                cluster.Add(article);
            }
        }

        return clusters;
    }

    private async Task<CuratedItemInsightDto> GenerateClusterInsightAsync(List<WebSearchResultDto> cluster, string keyword)
    {
        var clusterText = string.Join("\n\n", cluster.Select(a => $"{a.Title}\n{a.Snippet}"));
        var result = await _documentAnalyzer.GenerateCurationInsightAsync(clusterText, keyword);

        if (!result.IsSuccess || result.Data == null)
        {
            _logger.LogWarning("AI curation failed for keyword {Keyword}: {Error}", keyword, result.Error);
            return new CuratedItemInsightDto
            {
                KeyFact = cluster[0].Title,
                WhyItMatters = $"Potential impact on {keyword} monitoring.",
                Significance = 3
            };
        }

        return result.Data;
    }

    private string BuildHeadlineInsight(List<CuratedItemDto> items, string keyword)
    {
        if (items.Count == 0)
            return $"No significant signals detected for {keyword}.";

        var top = items[0];
        var topKeyword = top.ClusterKeywords.FirstOrDefault() ?? keyword;
        return $"Detected {items.Count} distinct signals for {keyword}. Top theme: {topKeyword}.";
    }

    private List<string> ExtractClusterKeywords(List<WebSearchResultDto> cluster)
    {
        var words = string.Join(" ", cluster.Select(c => c.Title))
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(w => NormalizeToken(w))
            .Where(w => w.Length > 4)
            .GroupBy(w => w)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => g.Key)
            .ToList();

        return words;
    }

    private string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var chars = input.ToLowerInvariant()
            .Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
            .ToArray();
        return string.Join(' ', new string(chars).Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private string NormalizeToken(string token)
    {
        return new string(token.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
    }

    private double TitleSimilarity(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            return 0;

        var distance = LevenshteinDistance(a, b);
        var maxLen = Math.Max(a.Length, b.Length);
        return maxLen == 0 ? 1 : 1.0 - (double)distance / maxLen;
    }

    private int LevenshteinDistance(string a, string b)
    {
        var lenA = a.Length;
        var lenB = b.Length;
        var dp = new int[lenA + 1, lenB + 1];

        for (int i = 0; i <= lenA; i++) dp[i, 0] = i;
        for (int j = 0; j <= lenB; j++) dp[0, j] = j;

        for (int i = 1; i <= lenA; i++)
        {
            for (int j = 1; j <= lenB; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                dp[i, j] = Math.Min(
                    Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                    dp[i - 1, j - 1] + cost);
            }
        }

        return dp[lenA, lenB];
    }
}
