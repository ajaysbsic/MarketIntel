namespace Alfanar.MarketIntel.Application.DTOs;

/// <summary>
/// Filter DTO for querying financial reports
/// </summary>
public class ReportFilterDto
{
    public string? CompanyName { get; set; }
    public string? ReportType { get; set; }
    public int? FiscalYear { get; set; }
    public string? FiscalQuarter { get; set; }
    public string? Sector { get; set; }
    public string? Region { get; set; }
    public string? ProcessingStatus { get; set; }
    public bool? IsProcessed { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
