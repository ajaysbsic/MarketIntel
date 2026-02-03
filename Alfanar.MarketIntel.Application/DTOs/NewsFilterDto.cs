namespace Alfanar.MarketIntel.Application.DTOs;

public class NewsFilterDto
{
    public string? Category { get; set; }
    public string? Region { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? SearchTerm { get; set; }
    public List<string>? Tags { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
