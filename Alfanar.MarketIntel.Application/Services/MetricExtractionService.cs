using Alfanar.MarketIntel.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Alfanar.MarketIntel.Application.Services;

/// <summary>
/// Extracts financial metrics from report text using regex patterns and AI
/// </summary>
public class MetricExtractionService
{
    private readonly ILogger<MetricExtractionService> _logger;

    public MetricExtractionService(ILogger<MetricExtractionService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Extract all metrics from a financial report
    /// </summary>
    public List<FinancialMetric> ExtractMetrics(FinancialReport report)
    {
        var metrics = new List<FinancialMetric>();

        if (string.IsNullOrWhiteSpace(report.ExtractedText))
        {
            _logger.LogWarning("No text to extract metrics from for report {ReportId}", report.Id);
            return metrics;
        }

        var text = report.ExtractedText;

        // Extract Revenue
        metrics.AddRange(ExtractRevenue(report.Id, text, report.FiscalQuarter, report.FiscalYear));

        // Extract Margins
        metrics.AddRange(ExtractMargins(report.Id, text, report.FiscalQuarter, report.FiscalYear));

        // Extract Growth Rates
        metrics.AddRange(ExtractGrowthRates(report.Id, text, report.FiscalQuarter, report.FiscalYear));

        // Extract EBITDA
        metrics.AddRange(ExtractEBITDA(report.Id, text, report.FiscalQuarter, report.FiscalYear));

        _logger.LogInformation("Extracted {Count} metrics from report {ReportId}", metrics.Count, report.Id);

        return metrics;
    }

    private List<FinancialMetric> ExtractRevenue(Guid reportId, string text, string? quarter, int? year)
    {
        var metrics = new List<FinancialMetric>();

        // Pattern: "revenue of $X.X billion" or "revenues of $X.X million"
        var patterns = new[]
        {
            @"revenue[s]?\s+(?:of|was|reached|totaled|grew to)?\s*\$?([\d,]+\.?\d*)\s*(billion|million|thousand)",
            @"\$?([\d,]+\.?\d*)\s*(billion|million)\s+in\s+revenue",
            @"revenue.*?\$?([\d,]+\.?\d*)\s*(billion|million)",
            @"sales.*?\$?([\d,]+\.?\d*)\s*(billion|million)"
        };

        foreach (var pattern in patterns)
        {
            var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                if (match.Success && match.Groups.Count >= 3)
                {
                    var valueStr = match.Groups[1].Value.Replace(",", "");
                    var unit = match.Groups[2].Value.ToLower();

                    if (decimal.TryParse(valueStr, out var value))
                    {
                        // Normalize to millions
                        var normalizedValue = unit == "billion" ? value * 1000 : value;

                        metrics.Add(new FinancialMetric
                        {
                            Id = Guid.NewGuid(),
                            FinancialReportId = reportId,
                            MetricType = "Revenue",
                            Value = normalizedValue,
                            Unit = "Million USD",
                            Period = FormatPeriod(quarter, year),
                            ConfidenceScore = 0.8,
                            ExtractionMethod = "Regex",
                            SourceText = match.Value,
                            ExtractedAt = DateTime.UtcNow
                        });

                        // Only take first match to avoid duplicates
                        break;
                    }
                }
            }
        }

        return metrics;
    }

    private List<FinancialMetric> ExtractMargins(Guid reportId, string text, string? quarter, int? year)
    {
        var metrics = new List<FinancialMetric>();

        // Pattern: "operating margin of X%" or "margin: X%"
        var patterns = new[]
        {
            @"operating\s+margin[s]?\s+(?:of|was|reached|improved to|declined to)?\s*([\d.]+)%",
            @"margin[s]?\s+(?:of|was|:)\s*([\d.]+)%",
            @"EBITDA\s+margin\s+(?:of|was)?\s*([\d.]+)%",
            @"net\s+margin\s+(?:of|was)?\s*([\d.]+)%"
        };

        foreach (var pattern in patterns)
        {
            var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                if (match.Success && match.Groups.Count >= 2)
                {
                    var valueStr = match.Groups[1].Value;

                    if (decimal.TryParse(valueStr, out var value))
                    {
                        var metricType = "Operating Margin";
                        if (match.Value.Contains("EBITDA", StringComparison.OrdinalIgnoreCase))
                            metricType = "EBITDA Margin";
                        else if (match.Value.Contains("net", StringComparison.OrdinalIgnoreCase))
                            metricType = "Net Margin";

                        metrics.Add(new FinancialMetric
                        {
                            Id = Guid.NewGuid(),
                            FinancialReportId = reportId,
                            MetricType = metricType,
                            Value = value,
                            Unit = "Percent",
                            Period = FormatPeriod(quarter, year),
                            ConfidenceScore = 0.75,
                            ExtractionMethod = "Regex",
                            SourceText = match.Value,
                            ExtractedAt = DateTime.UtcNow
                        });

                        break;
                    }
                }
            }
        }

        return metrics;
    }

    private List<FinancialMetric> ExtractGrowthRates(Guid reportId, string text, string? quarter, int? year)
    {
        var metrics = new List<FinancialMetric>();

        // Pattern: "growth of X%" or "increased X%" or "up X%"
        var patterns = new[]
        {
            @"(?:revenue|sales)\s+(?:growth|grew|increased|up)\s+(?:by\s+)?([\d.]+)%",
            @"(?:growth|grew|increased|up)\s+([\d.]+)%\s+(?:year-over-year|YoY|y-o-y)",
            @"([\d.]+)%\s+(?:growth|increase)"
        };

        foreach (var pattern in patterns)
        {
            var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                if (match.Success && match.Groups.Count >= 2)
                {
                    var valueStr = match.Groups[1].Value;

                    if (decimal.TryParse(valueStr, out var value))
                    {
                        metrics.Add(new FinancialMetric
                        {
                            Id = Guid.NewGuid(),
                            FinancialReportId = reportId,
                            MetricType = "Revenue Growth (YoY)",
                            Value = value,
                            Unit = "Percent",
                            Period = FormatPeriod(quarter, year),
                            ConfidenceScore = 0.7,
                            ExtractionMethod = "Regex",
                            SourceText = match.Value,
                            ExtractedAt = DateTime.UtcNow
                        });

                        break;
                    }
                }
            }
        }

        return metrics;
    }

    private List<FinancialMetric> ExtractEBITDA(Guid reportId, string text, string? quarter, int? year)
    {
        var metrics = new List<FinancialMetric>();

        // Pattern: "EBITDA of $X.X billion" or "EBITDA: $X.X million"
        var patterns = new[]
        {
            @"EBITDA\s+(?:of|was|:)?\s*\$?([\d,]+\.?\d*)\s*(billion|million)",
            @"adjusted\s+EBITDA\s+(?:of|was|:)?\s*\$?([\d,]+\.?\d*)\s*(billion|million)"
        };

        foreach (var pattern in patterns)
        {
            var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                if (match.Success && match.Groups.Count >= 3)
                {
                    var valueStr = match.Groups[1].Value.Replace(",", "");
                    var unit = match.Groups[2].Value.ToLower();

                    if (decimal.TryParse(valueStr, out var value))
                    {
                        var normalizedValue = unit == "billion" ? value * 1000 : value;

                        metrics.Add(new FinancialMetric
                        {
                            Id = Guid.NewGuid(),
                            FinancialReportId = reportId,
                            MetricType = "EBITDA",
                            Value = normalizedValue,
                            Unit = "Million USD",
                            Period = FormatPeriod(quarter, year),
                            ConfidenceScore = 0.75,
                            ExtractionMethod = "Regex",
                            SourceText = match.Value,
                            ExtractedAt = DateTime.UtcNow
                        });

                        break;
                    }
                }
            }
        }

        return metrics;
    }

    private string FormatPeriod(string? quarter, int? year)
    {
        if (year.HasValue && !string.IsNullOrWhiteSpace(quarter))
            return $"{quarter} {year}";
        if (year.HasValue)
            return $"FY {year}";
        return "Unknown";
    }
}
