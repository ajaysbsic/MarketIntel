using Alfanar.MarketIntel.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Alfanar.MarketIntel.Application.Services;

public class RuleBasedCategoryClassifier : ICategoryClassifier
{
    private readonly ILogger<RuleBasedCategoryClassifier> _logger;
    private static readonly string[] separator = new[] { ". ", "! ", "? " };

    public RuleBasedCategoryClassifier(ILogger<RuleBasedCategoryClassifier> logger)
    {
        _logger = logger;
    }

    public Task<(string Category, string Summary, double Confidence)> ClassifyAndSummarizeAsync(
        string title, 
        string bodyText)
    {
        var text = $"{title} {bodyText}".ToLowerInvariant();
        var (category, confidence) = ClassifyText(text);
        var summary = GenerateSummary(title, bodyText);

        _logger.LogInformation("Classified article as {Category} with confidence {Confidence}", 
            category, confidence);

        return Task.FromResult((category, summary, confidence));
    }

    private (string Category, double Confidence) ClassifyText(string text)
    {
        var scores = new Dictionary<string, double>
        {
            ["M&A"] = CalculateCategoryScore(text, new[]
            {
                ("acquisition", 2.0), ("merger", 2.0), ("acquired", 2.0), 
                ("buyout", 1.5), ("takeover", 1.5), ("purchase", 1.0)
            }),
            ["Funding"] = CalculateCategoryScore(text, new[]
            {
                ("funding", 2.0), ("investment", 2.0), ("raised", 2.0),
                ("series a", 2.5), ("series b", 2.5), ("series c", 2.5),
                ("venture capital", 2.0), ("$", 1.0), ("million", 1.0)
            }),
            ["Policy"] = CalculateCategoryScore(text, new[]
            {
                ("policy", 2.0), ("regulation", 2.0), ("government", 1.5),
                ("tender", 2.0), ("legislation", 2.0), ("law", 1.0),
                ("mandate", 1.5), ("compliance", 1.5)
            }),
            ["Project"] = CalculateCategoryScore(text, new[]
            {
                ("project", 2.0), ("commissioned", 2.0), ("deployed", 2.0),
                ("installation", 1.5), ("mw", 2.0), ("mwh", 2.0),
                ("charging station", 2.0), ("infrastructure", 1.5)
            }),
            ["Technology"] = CalculateCategoryScore(text, new[]
            {
                ("battery", 2.0), ("technology", 1.5), ("innovation", 1.5),
                ("patent", 2.0), ("breakthrough", 2.0), ("research", 1.5)
            })
        };

        var maxScore = scores.Max(x => x.Value);
        if (maxScore < 0.5)
            return ("MarketMetrics", 0.3);

        var category = scores.First(x => x.Value == maxScore).Key;
        var confidence = Math.Min(maxScore / 10.0, 0.95); // Normalize to 0-0.95 range

        return (category, confidence);
    }

    private double CalculateCategoryScore(string text, (string keyword, double weight)[] keywords)
    {
        double score = 0;
        foreach (var (keyword, weight) in keywords)
        {
            var count = CountOccurrences(text, keyword);
            score += count * weight;
        }
        return score;
    }

    private int CountOccurrences(string text, string keyword)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(keyword, index, StringComparison.OrdinalIgnoreCase)) != -1)
        {
            count++;
            index += keyword.Length;
        }
        return count;
    }

    private string GenerateSummary(string title, string bodyText)
    {
        // If we have body text, try to extract first meaningful sentences
        if (!string.IsNullOrWhiteSpace(bodyText) && bodyText.Length > 100)
        {
            var sentences = bodyText.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            if (sentences.Length > 0)
            {
                // Take first 2-3 sentences, max 300 chars
                var summary = string.Join(". ", sentences.Take(3));
                if (summary.Length > 300)
                    summary = summary.Substring(0, 297) + "...";
                else
                    summary += ".";
                return summary;
            }
        }

        // Fallback: use title and first 200 chars of body
        var text = !string.IsNullOrWhiteSpace(bodyText) ? bodyText : title;
        return text.Length <= 240 ? text : text.Substring(0, 237) + "...";
    }
}
