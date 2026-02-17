using System.Text.Json.Serialization;

namespace Alfanar.MarketIntel.Application.DTOs;

public class AlertConfirmationDto
{
    [JsonPropertyName("is_match")]
    public bool IsMatch { get; set; }

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("details")]
    public string? Details { get; set; }
}

public class EvaluateArticleRequestDto
{
    public string Title { get; set; } = string.Empty;

    public string? Snippet { get; set; }

    public string? BodyText { get; set; }

    public string SourceType { get; set; } = string.Empty;

    public Guid SourceId { get; set; }

    public string? SourceUrl { get; set; }
}
