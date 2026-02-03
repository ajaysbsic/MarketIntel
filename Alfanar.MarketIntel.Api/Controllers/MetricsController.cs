using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Alfanar.MarketIntel.Api.Controllers;

[ApiController]
[Route("api/metrics")]
public class MetricsController : ControllerBase
{
    private readonly IFinancialMetricRepository _metricRepository;
    private readonly ILogger<MetricsController> _logger;

    public MetricsController(
        IFinancialMetricRepository metricRepository,
        ILogger<MetricsController> logger)
    {
        _metricRepository = metricRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get metrics for a specific company
    /// </summary>
    [HttpGet("company/{companyName}")]
    public async Task<IActionResult> GetByCompany(string companyName, [FromQuery] string? metricType = null)
    {
        try
        {
            var metrics = await _metricRepository.GetByCompanyAsync(companyName, metricType);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving metrics for company {Company}", companyName);
            return StatusCode(500, new { message = "Error retrieving metrics" });
        }
    }

    /// <summary>
    /// Get time-series data for a specific metric
    /// </summary>
    [HttpGet("timeseries")]
    public async Task<IActionResult> GetTimeSeries(
        [FromQuery] string companyName,
        [FromQuery] string metricType,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var metrics = await _metricRepository.GetTimeSeriesAsync(companyName, metricType, fromDate, toDate);

            // Format for charting
            var chartData = new
            {
                labels = metrics.Select(m => m.Period).ToList(),
                data = metrics.Select(m => m.Value).ToList(),
                metricType = metricType,
                company = companyName,
                unit = metrics.FirstOrDefault()?.Unit ?? "Unknown"
            };

            return Ok(chartData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving time series for {Company} - {Metric}", companyName, metricType);
            return StatusCode(500, new { message = "Error retrieving time series" });
        }
    }

    /// <summary>
    /// Get metrics summary for dashboard
    /// </summary>
    [HttpGet("summary/{companyName}")]
    public async Task<IActionResult> GetSummary(string companyName)
    {
        try
        {
            var allMetrics = await _metricRepository.GetByCompanyAsync(companyName);

            // Get latest of each metric type
            var summary = allMetrics
                .GroupBy(m => m.MetricType)
                .Select(g => new
                {
                    metricType = g.Key,
                    latestValue = g.OrderByDescending(m => m.ExtractedAt).First().Value,
                    unit = g.First().Unit,
                    period = g.OrderByDescending(m => m.ExtractedAt).First().Period,
                    change = g.OrderByDescending(m => m.ExtractedAt).First().ChangePercent
                })
                .ToList();

            return Ok(new
            {
                company = companyName,
                metrics = summary,
                lastUpdated = allMetrics.Max(m => m.ExtractedAt)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving summary for {Company}", companyName);
            return StatusCode(500, new { message = "Error retrieving summary" });
        }
    }

    /// <summary>
    /// Get metrics for a specific report
    /// </summary>
    [HttpGet("report/{reportId:guid}")]
    public async Task<IActionResult> GetByReport(Guid reportId)
    {
        try
        {
            var metrics = await _metricRepository.GetByReportIdAsync(reportId);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving metrics for report {ReportId}", reportId);
            return StatusCode(500, new { message = "Error retrieving metrics" });
        }
    }
}
