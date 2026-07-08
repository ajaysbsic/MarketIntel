namespace Alfanar.MarketIntel.Domain.Entities;

public class TenderNotificationRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Scope { get; set; } = "Global";
    public string? UserId { get; set; }
    public string Channels { get; set; } = "InApp";
    public string? CountryFilter { get; set; }
    public string? SectorFilter { get; set; }
    public string? AuthorityFilter { get; set; }
    /// <summary>
    /// Comma-separated list of company/entity name aliases (e.g. "SEC,Saudi Electricity,Water Authority").
    /// Matched against title, summary, authority name, and source name of the tender (case-insensitive contains).
    /// </summary>
    public string? EntityFilter { get; set; }
    public decimal? ValueMin { get; set; }
    public decimal? ValueMax { get; set; }
    public string? Keywords { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public ICollection<TenderNotificationLog> NotificationLogs { get; set; } = new List<TenderNotificationLog>();
}
