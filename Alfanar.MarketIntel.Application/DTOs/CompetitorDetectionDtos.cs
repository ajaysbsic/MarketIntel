using System.Text.Json.Serialization;

namespace Alfanar.MarketIntel.Application.DTOs;

public class CompetitorMentionInsightDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("context")]
    public string? Context { get; set; }

    [JsonPropertyName("sentiment_score")]
    public double? SentimentScore { get; set; }

    [JsonPropertyName("sentiment_label")]
    public string? SentimentLabel { get; set; }
}

public class CompetitorSuggestionDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("industry")]
    public string? Industry { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

public class CompetitorDetectionResultDto
{
    [JsonPropertyName("mentions")]
    public List<CompetitorMentionInsightDto> Mentions { get; set; } = new();

    [JsonPropertyName("new_competitors")]
    public List<CompetitorSuggestionDto> NewCompetitors { get; set; } = new();
}
