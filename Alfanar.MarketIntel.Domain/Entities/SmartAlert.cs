namespace Alfanar.MarketIntel.Domain.Entities;

/// <summary>
/// Smart alerts triggered by business rules
/// </summary>
public class SmartAlert
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Reference to the financial report that triggered the alert
    /// </summary>
    public Guid? FinancialReportId { get; set; }
    public FinancialReport? FinancialReport { get; set; }
    
    /// <summary>
    /// Alert type (MarginDrop, RevenueGrowth, RiskMention, OpportunityDetected, etc.)
    /// </summary>
    public string AlertType { get; set; } = default!;
    
    /// <summary>
    /// Severity (Critical, High, Medium, Low, Info)
    /// </summary>
    public string Severity { get; set; } = default!;
    
    /// <summary>
    /// Alert title/headline
    /// </summary>
    public string Title { get; set; } = default!;
    
    /// <summary>
    /// Detailed message
    /// </summary>
    public string Message { get; set; } = default!;
    
    /// <summary>
    /// Company name
    /// </summary>
    public string CompanyName { get; set; } = default!;
    
    /// <summary>
    /// Metric that triggered the alert (if applicable)
    /// </summary>
    public string? TriggerMetric { get; set; }
    
    /// <summary>
    /// Threshold value that was crossed
    /// </summary>
    public decimal? ThresholdValue { get; set; }
    
    /// <summary>
    /// Actual value that triggered the alert
    /// </summary>
    public decimal? ActualValue { get; set; }
    
    /// <summary>
    /// Keywords or patterns that triggered the alert
    /// </summary>
    public string? TriggerKeywords { get; set; }
    
    /// <summary>
    /// Whether the alert has been acknowledged by a user
    /// </summary>
    public bool IsAcknowledged { get; set; } = false;
    
    /// <summary>
    /// When the alert was acknowledged
    /// </summary>
    public DateTime? AcknowledgedAt { get; set; }
    
    /// <summary>
    /// User who acknowledged the alert
    /// </summary>
    public string? AcknowledgedBy { get; set; }
    
    /// <summary>
    /// When the alert was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Additional metadata as JSON
    /// </summary>
    public string? Metadata { get; set; }
}
