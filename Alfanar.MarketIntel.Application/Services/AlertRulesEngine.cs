using Alfanar.MarketIntel.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Alfanar.MarketIntel.Application.Services;

/// <summary>
/// Evaluates business rules and triggers smart alerts
/// </summary>
public class AlertRulesEngine
{
    private readonly ILogger<AlertRulesEngine> _logger;

    // Risk keywords to monitor
    private readonly string[] _riskKeywords = new[]
    {
        "lawsuit", "litigation", "investigation", "regulatory",
        "default", "bankruptcy", "restructuring", "layoff",
        "supply chain", "disruption", "shortage", "delay",
        "cyber", "hack", "breach", "data loss",
        "inflation", "recession", "downturn", "headwind",
        "challenge", "pressure", "concern", "risk",
        "decline", "drop", "decrease", "lower than expected"
    };

    // Opportunity keywords
    private readonly string[] _opportunityKeywords = new[]
    {
        "expansion", "growth", "acquisition", "merger",
        "new market", "partnership", "contract", "deal",
        "innovation", "breakthrough", "launch", "product",
        "investment", "funding", "capital", "revenue increase"
    };

    public AlertRulesEngine(ILogger<AlertRulesEngine> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Evaluate all alert rules for a report
    /// </summary>
    public List<SmartAlert> EvaluateRules(FinancialReport report, List<FinancialMetric> metrics)
    {
        var alerts = new List<SmartAlert>();

        // Rule 1: Margin Drop > 1%
        alerts.AddRange(CheckMarginDrop(report, metrics));

        // Rule 2: Revenue Decline
        alerts.AddRange(CheckRevenueDrop(report, metrics));

        // Rule 3: New Risk Mentions
        alerts.AddRange(CheckRiskMentions(report));

        // Rule 4: Opportunity Detection
        alerts.AddRange(CheckOpportunities(report));

        // Rule 5: Significant Growth
        alerts.AddRange(CheckSignificantGrowth(report, metrics));

        _logger.LogInformation("Generated {Count} alerts for report {ReportId}", alerts.Count, report.Id);

        return alerts;
    }

    /// <summary>
    /// Check for margin drops > 1%
    /// </summary>
    private List<SmartAlert> CheckMarginDrop(FinancialReport report, List<FinancialMetric> metrics)
    {
        var alerts = new List<SmartAlert>();

        var marginMetrics = metrics.Where(m => 
            m.MetricType.Contains("Margin", StringComparison.OrdinalIgnoreCase) && 
            m.Value.HasValue)
            .ToList();

        foreach (var metric in marginMetrics)
        {
            // Look for text mentioning previous period
            if (TryExtractPreviousMargin(report.ExtractedText, out var previousMargin))
            {
                var currentMargin = metric.Value!.Value;
                var drop = previousMargin - currentMargin;

                if (drop > 1.0m)
                {
                    alerts.Add(new SmartAlert
                    {
                        Id = Guid.NewGuid(),
                        FinancialReportId = report.Id,
                        AlertType = "MarginDrop",
                        Severity = drop > 3.0m ? "Critical" : "High",
                        Title = $"?? {report.CompanyName}: {metric.MetricType} Dropped {drop:F1}%",
                        Message = $"The {metric.MetricType} declined from {previousMargin:F1}% to {currentMargin:F1}%, a drop of {drop:F1} percentage points. This may indicate pricing pressure or cost increases.",
                        CompanyName = report.CompanyName,
                        TriggerMetric = metric.MetricType,
                        ThresholdValue = 1.0m,
                        ActualValue = drop,
                        CreatedAt = DateTime.UtcNow
                    });

                    _logger.LogWarning("{Company} margin dropped by {Drop}%", report.CompanyName, drop);
                }
            }
        }

        return alerts;
    }

    /// <summary>
    /// Check for revenue decline
    /// </summary>
    private List<SmartAlert> CheckRevenueDrop(FinancialReport report, List<FinancialMetric> metrics)
    {
        var alerts = new List<SmartAlert>();

        var revenueMetrics = metrics.Where(m => 
            m.MetricType.Equals("Revenue", StringComparison.OrdinalIgnoreCase) && 
            m.Value.HasValue)
            .ToList();

        foreach (var metric in revenueMetrics)
        {
            var text = report.ExtractedText ?? "";
            
            // Check for negative growth mentions
            var declinePatterns = new[]
            {
                @"revenue\s+(?:declined|decreased|dropped|fell)\s+(?:by\s+)?([\d.]+)%",
                @"(?:down|decrease of)\s+([\d.]+)%\s+(?:in|from)\s+revenue"
            };

            foreach (var pattern in declinePatterns)
            {
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                if (match.Success && decimal.TryParse(match.Groups[1].Value, out var declinePercent))
                {
                    alerts.Add(new SmartAlert
                    {
                        Id = Guid.NewGuid(),
                        FinancialReportId = report.Id,
                        AlertType = "RevenueDrop",
                        Severity = declinePercent > 10 ? "Critical" : "High",
                        Title = $"?? {report.CompanyName}: Revenue Declined {declinePercent:F1}%",
                        Message = $"Revenue decreased by {declinePercent:F1}%. Current revenue: ${metric.Value:N0}M. This may signal market headwinds or competitive pressure.",
                        CompanyName = report.CompanyName,
                        TriggerMetric = "Revenue",
                        ActualValue = metric.Value,
                        CreatedAt = DateTime.UtcNow
                    });

                    break;
                }
            }
        }

        return alerts;
    }

    /// <summary>
    /// Check for new risk mentions
    /// </summary>
    private List<SmartAlert> CheckRiskMentions(FinancialReport report)
    {
        var alerts = new List<SmartAlert>();

        if (string.IsNullOrWhiteSpace(report.ExtractedText))
            return alerts;

        var text = report.ExtractedText.ToLower();
        var foundKeywords = new List<string>();

        foreach (var keyword in _riskKeywords)
        {
            if (text.Contains(keyword.ToLower()))
            {
                foundKeywords.Add(keyword);
            }
        }

        // Group common themes
        var criticalRisks = foundKeywords.Where(k => 
            new[] { "lawsuit", "bankruptcy", "default", "breach", "hack" }.Contains(k)).ToList();
        
        var operationalRisks = foundKeywords.Where(k => 
            new[] { "supply chain", "disruption", "shortage", "delay" }.Contains(k)).ToList();

        var economicRisks = foundKeywords.Where(k => 
            new[] { "inflation", "recession", "downturn", "headwind" }.Contains(k)).ToList();

        if (criticalRisks.Any())
        {
            alerts.Add(new SmartAlert
            {
                Id = Guid.NewGuid(),
                FinancialReportId = report.Id,
                AlertType = "CriticalRisk",
                Severity = "Critical",
                Title = $"?? {report.CompanyName}: Critical Risk Detected",
                Message = $"Report mentions critical risk factors: {string.Join(", ", criticalRisks)}. Immediate attention recommended.",
                CompanyName = report.CompanyName,
                TriggerKeywords = string.Join(", ", criticalRisks),
                CreatedAt = DateTime.UtcNow
            });
        }

        if (operationalRisks.Any())
        {
            alerts.Add(new SmartAlert
            {
                Id = Guid.NewGuid(),
                FinancialReportId = report.Id,
                AlertType = "OperationalRisk",
                Severity = "High",
                Title = $"?? {report.CompanyName}: Operational Challenges",
                Message = $"Report highlights operational concerns: {string.Join(", ", operationalRisks)}. May impact near-term performance.",
                CompanyName = report.CompanyName,
                TriggerKeywords = string.Join(", ", operationalRisks),
                CreatedAt = DateTime.UtcNow
            });
        }

        if (economicRisks.Any())
        {
            alerts.Add(new SmartAlert
            {
                Id = Guid.NewGuid(),
                FinancialReportId = report.Id,
                AlertType = "MacroRisk",
                Severity = "Medium",
                Title = $"?? {report.CompanyName}: Economic Headwinds Mentioned",
                Message = $"Management cites macroeconomic challenges: {string.Join(", ", economicRisks)}.",
                CompanyName = report.CompanyName,
                TriggerKeywords = string.Join(", ", economicRisks),
                CreatedAt = DateTime.UtcNow
            });
        }

        return alerts;
    }

    /// <summary>
    /// Check for opportunity mentions
    /// </summary>
    private List<SmartAlert> CheckOpportunities(FinancialReport report)
    {
        var alerts = new List<SmartAlert>();

        if (string.IsNullOrWhiteSpace(report.ExtractedText))
            return alerts;

        var text = report.ExtractedText.ToLower();
        var foundKeywords = new List<string>();

        foreach (var keyword in _opportunityKeywords)
        {
            if (text.Contains(keyword.ToLower()))
            {
                foundKeywords.Add(keyword);
            }
        }

        if (foundKeywords.Count >= 3) // Significant opportunity signals
        {
            alerts.Add(new SmartAlert
            {
                Id = Guid.NewGuid(),
                FinancialReportId = report.Id,
                AlertType = "OpportunityDetected",
                Severity = "Info",
                Title = $"?? {report.CompanyName}: Growth Opportunities Identified",
                Message = $"Report highlights multiple growth initiatives: {string.Join(", ", foundKeywords.Take(5))}. Potential for expansion.",
                CompanyName = report.CompanyName,
                TriggerKeywords = string.Join(", ", foundKeywords),
                CreatedAt = DateTime.UtcNow
            });
        }

        // Specific check for M&A activity
        var maKeywords = foundKeywords.Where(k => 
            new[] { "acquisition", "merger", "deal", "partnership" }.Contains(k)).ToList();

        if (maKeywords.Any())
        {
            alerts.Add(new SmartAlert
            {
                Id = Guid.NewGuid(),
                FinancialReportId = report.Id,
                AlertType = "MergerAcquisition",
                Severity = "High",
                Title = $"?? {report.CompanyName}: M&A Activity Detected",
                Message = $"Report discusses {string.Join(", ", maKeywords)}. This could significantly impact valuation and strategy.",
                CompanyName = report.CompanyName,
                TriggerKeywords = string.Join(", ", maKeywords),
                CreatedAt = DateTime.UtcNow
            });
        }

        return alerts;
    }

    /// <summary>
    /// Check for significant growth
    /// </summary>
    private List<SmartAlert> CheckSignificantGrowth(FinancialReport report, List<FinancialMetric> metrics)
    {
        var alerts = new List<SmartAlert>();

        var growthMetrics = metrics.Where(m => 
            m.MetricType.Contains("Growth", StringComparison.OrdinalIgnoreCase) && 
            m.Value.HasValue)
            .ToList();

        foreach (var metric in growthMetrics)
        {
            var growthRate = metric.Value!.Value;

            if (growthRate > 20) // >20% growth
            {
                alerts.Add(new SmartAlert
                {
                    Id = Guid.NewGuid(),
                    FinancialReportId = report.Id,
                    AlertType = "StrongGrowth",
                    Severity = "Info",
                    Title = $"?? {report.CompanyName}: Strong Growth of {growthRate:F1}%",
                    Message = $"{metric.MetricType} shows exceptional growth of {growthRate:F1}%. Company outperforming expectations.",
                    CompanyName = report.CompanyName,
                    TriggerMetric = metric.MetricType,
                    ThresholdValue = 20.0m,
                    ActualValue = growthRate,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        return alerts;
    }

    /// <summary>
    /// Try to extract previous margin from text
    /// </summary>
    private bool TryExtractPreviousMargin(string? text, out decimal previousMargin)
    {
        previousMargin = 0;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        // Patterns like "from 18.5% to 16.2%" or "compared to 19.1%"
        var patterns = new[]
        {
            @"from\s+([\d.]+)%\s+to\s+([\d.]+)%",
            @"compared\s+to\s+([\d.]+)%",
            @"versus\s+([\d.]+)%",
            @"prior\s+(?:year|quarter|period)\s+(?:of\s+)?([\d.]+)%"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var valueStr = match.Groups[1].Value;
                if (decimal.TryParse(valueStr, out previousMargin))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
