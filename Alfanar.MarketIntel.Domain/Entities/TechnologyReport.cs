namespace Alfanar.MarketIntel.Domain.Entities;

public class TechnologyReport
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;

    public string Keywords { get; set; } = string.Empty; // JSON array

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;

    public string? GeneratedBy { get; set; }

    public string? PdfFilePath { get; set; }

    public int TotalResults { get; set; }

    public string? Summary { get; set; } // AI-generated summary - phase 2

    // Navigation
    public ICollection<ReportResult> ReportResults { get; set; } = new List<ReportResult>();
}
