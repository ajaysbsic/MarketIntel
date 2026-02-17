using Alfanar.MarketIntel.Application.DTOs;
using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Alfanar.MarketIntel.Application.Services;

public class ArticleAlertEngine
{
    private readonly IDocumentAnalyzer _documentAnalyzer;
    private readonly IConfiguration _configuration;
    private readonly TechThreatDetector _techThreatDetector;
    private readonly ILogger<ArticleAlertEngine> _logger;

    private readonly Dictionary<string, string[]> _keywordTriggers = new()
    {
        { "MergerAcquisition", new[] { "acquire", "acquisition", "merger", "takeover", "buyout", "deal", "consolidation" } },
        { "FundingAnnouncement", new[] { "funding", "investment", "series", "raised", "capital", "ipo", "valuation" } },
        { "LeadershipChange", new[] { "ceo", "cto", "cfo", "appointed", "resigned", "hired", "board", "executive" } },
        { "RegulatoryMention", new[] { "regulation", "compliance", "policy", "government", "sanction", "tariff", "ban" } },
        { "CompetitorActivity", new[] { "competitor", "rival", "market share", "expansion", "partnership" } },
        { "MarketShift", new[] { "market shift", "demand", "pricing", "supply", "headwind", "tailwind" } }
    };

    private readonly Dictionary<string, string> _aiPrompts = new()
    {
        { "MergerAcquisition", "Is this article about an actual M&A event? Identify the parties and stage." },
        { "FundingAnnouncement", "Is this about a funding event? Identify amount, company, and stage." },
        { "LeadershipChange", "Is this about an executive change? Identify who, role, and company." },
        { "RegulatoryMention", "Is this about a regulatory action affecting the market? Summarize impact." },
        { "CompetitorActivity", "Is this about competitor activity? Identify competitor and action." },
        { "MarketShift", "Is this a material market shift? Summarize the shift and implication." }
    };

    public ArticleAlertEngine(
        IDocumentAnalyzer documentAnalyzer,
        IConfiguration configuration,
        TechThreatDetector techThreatDetector,
        ILogger<ArticleAlertEngine> logger)
    {
        _documentAnalyzer = documentAnalyzer;
        _configuration = configuration;
        _techThreatDetector = techThreatDetector;
        _logger = logger;
    }

    public async Task<List<SmartAlert>> EvaluateArticleAsync(
        string title,
        string snippet,
        string? bodyText,
        string sourceType,
        Guid sourceId,
        string? sourceUrl)
    {
        var alerts = new List<SmartAlert>();
        var enabled = _configuration.GetValue("Alerts:EnableArticleAlerts", true);
        if (!enabled)
            return alerts;

        var content = string.Join("\n", new[] { title, snippet, bodyText ?? string.Empty });
        if (string.IsNullOrWhiteSpace(content))
            return alerts;

        foreach (var alertType in _keywordTriggers.Keys)
        {
            var triggers = _keywordTriggers[alertType];
            var matched = triggers.Where(t => content.Contains(t, StringComparison.OrdinalIgnoreCase)).ToList();
            if (matched.Count == 0)
                continue;

            var confirmation = await ConfirmAlertAsync(alertType, content);
            if (confirmation != null && !confirmation.IsMatch)
                continue;

            var confidence = confirmation?.Confidence ?? 0.6;
            var severity = MapSeverity(alertType, confidence);

            alerts.Add(new SmartAlert
            {
                Id = Guid.NewGuid(),
                AlertType = alertType,
                Severity = severity,
                Title = $"{alertType}: {title}",
                Message = confirmation?.Details ?? snippet,
                CompanyName = "Market",
                TriggerKeywords = string.Join(", ", matched),
                SourceType = sourceType,
                SourceId = sourceId,
                SourceUrl = sourceUrl,
                CreatedAt = DateTime.UtcNow
            });
        }

        var techThreat = await _techThreatDetector.DetectTechThreatAsync(title, snippet, bodyText);
        if (techThreat.IsThreat)
        {
            alerts.Add(new SmartAlert
            {
                Id = Guid.NewGuid(),
                AlertType = "TechnologyThreat",
                AlertSubType = techThreat.SubType,
                Severity = techThreat.Confidence >= 0.9 ? "Critical" : "High",
                Title = $"Technology threat detected: {techThreat.Reason}",
                Message = $"Article: {title}\nReason: {techThreat.Reason}",
                CompanyName = "Market",
                TriggerKeywords = techThreat.TechKeyword,
                SourceType = sourceType,
                SourceId = sourceId,
                SourceUrl = sourceUrl,
                CreatedAt = DateTime.UtcNow,
                Metadata = $"{{\"confidence\":{techThreat.Confidence:0.00}}}"
            });
        }

        return alerts;
    }

    private async Task<AlertConfirmationDto?> ConfirmAlertAsync(string alertType, string content)
    {
        var useAi = _configuration.GetValue("Alerts:AiConfirmation", true);
        if (!useAi || !_documentAnalyzer.IsAvailable())
            return null;

        if (!_aiPrompts.TryGetValue(alertType, out var prompt))
            return null;

        var result = await _documentAnalyzer.ConfirmAlertAsync(content, alertType, prompt);
        if (!result.IsSuccess || result.Data == null)
        {
            _logger.LogWarning("AI confirmation failed for {AlertType}: {Error}", alertType, result.Error);
            return null;
        }

        return result.Data;
    }

    private static string MapSeverity(string alertType, double confidence)
    {
        if (alertType == "MergerAcquisition" && confidence >= 0.85)
            return "Critical";

        if (confidence >= 0.8)
            return "High";

        if (confidence >= 0.6)
            return "Medium";

        return "Low";
    }
}
