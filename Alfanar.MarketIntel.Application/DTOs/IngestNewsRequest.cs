namespace Alfanar.MarketIntel.Application.DTOs;

public class IngestNewsRequest
{
    public string Source { get; set; } = default!;
    public string Url { get; set; } = default!;
    public string Title { get; set; } = default!;
    public DateTime? PublishedUtc { get; set; }
    public string? Region { get; set; }
    public string? Summary { get; set; }
    public string? BodyText { get; set; }
    public List<string>? Tags { get; set; }
}
