using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Alfanar.MarketIntel.Application.Services;

public class TechThreatDetector
{
    private readonly ICompetitorRepository _competitorRepository;
    private readonly ILogger<TechThreatDetector> _logger;

    private readonly string[] _threatKeywords =
    {
        "ai", "artificial intelligence", "machine learning", "neural network", "gpt", "foundation model",
        "quantum", "blockchain", "edge computing", "digital transformation", "cloud migration",
        "automation", "robotics", "industrial iot", "cybersecurity"
    };

    private readonly string[] _adoptionKeywords =
    {
        "adopt", "adoption", "deploy", "deployment", "implement", "implementation", "launch", "pilot",
        "rollout", "partner", "partnership", "invest", "investment", "upgrade"
    };

    private readonly string[] _marketShiftKeywords =
    {
        "industry", "market-wide", "standard", "consortium", "regulator", "policy", "directive"
    };

    private readonly string[] _patentKeywords =
    {
        "patent", "intellectual property", "ip filing", "patent filing"
    };

    public TechThreatDetector(ICompetitorRepository competitorRepository, ILogger<TechThreatDetector> logger)
    {
        _competitorRepository = competitorRepository;
        _logger = logger;
    }

    public async Task<TechThreatResult> DetectTechThreatAsync(string title, string snippet, string? bodyText)
    {
        var content = string.Join(" ", new[] { title, snippet, bodyText ?? string.Empty });
        if (string.IsNullOrWhiteSpace(content))
        {
            return TechThreatResult.NoThreat();
        }

        var techKeyword = _threatKeywords.FirstOrDefault(k => content.Contains(k, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(techKeyword))
        {
            return TechThreatResult.NoThreat();
        }

        var adoptionMatch = _adoptionKeywords.Any(k => content.Contains(k, StringComparison.OrdinalIgnoreCase));
        var patentMatch = _patentKeywords.Any(k => content.Contains(k, StringComparison.OrdinalIgnoreCase));
        var marketShiftMatch = _marketShiftKeywords.Any(k => content.Contains(k, StringComparison.OrdinalIgnoreCase));

        var competitors = await _competitorRepository.GetAllAsync(includeInactive: false);
        var competitorName = competitors
            .Select(c => c.Name)
            .FirstOrDefault(name => content.Contains(name, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(competitorName) && adoptionMatch)
        {
            return new TechThreatResult(true, 0.95, AlertSubType.TechAdoption, techKeyword,
                $"Competitor {competitorName} adopting {techKeyword}");
        }

        if (patentMatch)
        {
            return new TechThreatResult(true, 0.8, AlertSubType.PatentInnovation, techKeyword,
                $"Patent or IP activity detected around {techKeyword}");
        }

        if (marketShiftMatch)
        {
            return new TechThreatResult(true, 0.75, AlertSubType.StandardsShift, techKeyword,
                $"Market-wide technology shift detected for {techKeyword}");
        }

        _logger.LogInformation("Tech keyword detected without strong threat signal: {Keyword}", techKeyword);
        return TechThreatResult.NoThreat();
    }
}

public record TechThreatResult(
    bool IsThreat,
    double Confidence,
    AlertSubType? SubType,
    string? TechKeyword,
    string Reason)
{
    public static TechThreatResult NoThreat()
    {
        return new TechThreatResult(false, 0, null, null, "No threat detected");
    }
}
