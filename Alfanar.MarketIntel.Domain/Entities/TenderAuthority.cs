namespace Alfanar.MarketIntel.Domain.Entities;

public class TenderAuthority
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public Guid CountryId { get; set; }
    public string AuthorityType { get; set; } = "Gov";
    public string NormalizedName { get; set; } = string.Empty;
    public string? AliasesJson { get; set; }

    public TenderCountry Country { get; set; } = default!;
    public ICollection<TenderNotice> Notices { get; set; } = new List<TenderNotice>();
}
