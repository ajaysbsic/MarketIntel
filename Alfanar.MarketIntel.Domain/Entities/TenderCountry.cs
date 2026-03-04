namespace Alfanar.MarketIntel.Domain.Entities;

public class TenderCountry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string IsoCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string RegionGroup { get; set; } = "MiddleEast";
    public bool IsActive { get; set; } = true;

    public ICollection<TenderAuthority> Authorities { get; set; } = new List<TenderAuthority>();
    public ICollection<TenderNotice> Notices { get; set; } = new List<TenderNotice>();
}
