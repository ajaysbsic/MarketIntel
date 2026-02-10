namespace Alfanar.MarketIntel.Domain.Entities;

// Join entity for many-to-many relationship between reports and tags
public class FinancialReportTag
{
    public Guid FinancialReportId { get; set; }
    public int TagId { get; set; }

    public FinancialReport FinancialReport { get; set; } = default!;
    public Tag Tag { get; set; } = default!;
}
