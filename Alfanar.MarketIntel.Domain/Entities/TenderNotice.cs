namespace Alfanar.MarketIntel.Domain.Entities;

public class TenderNotice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ExternalId { get; set; } = string.Empty;
    public Guid SourceId { get; set; }
    public Guid? AuthorityId { get; set; }
    public Guid CountryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? Sector { get; set; }
    public string? Category { get; set; }
    public DateTime? PublishDate { get; set; }
    public DateTime? Deadline { get; set; }
    public decimal? EstimatedValue { get; set; }
    public string? Currency { get; set; }
    public string SourceUrl { get; set; } = string.Empty;
    public string Status { get; set; } = "Open";
    public Guid? CurrentVersionId { get; set; }
    public string ContentHash { get; set; } = string.Empty;
    public DateTime FirstSeenAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    public DateTime LastChangedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public TenderSource Source { get; set; } = default!;
    public TenderAuthority? Authority { get; set; }
    public TenderCountry Country { get; set; } = default!;
    public TenderVersion? CurrentVersion { get; set; }
    public ICollection<TenderVersion> Versions { get; set; } = new List<TenderVersion>();
    public ICollection<TenderDocument> Documents { get; set; } = new List<TenderDocument>();
    public ICollection<TenderNotificationLog> NotificationLogs { get; set; } = new List<TenderNotificationLog>();
}
