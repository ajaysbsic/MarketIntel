using System.Text.Json.Serialization;

namespace Alfanar.MarketIntel.Application.DTOs;

/// <summary>
/// Request DTO for AI article curation
/// </summary>
public class CurateIntelligenceRequestDto
{
    public string Keyword { get; set; } = string.Empty;

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public int MaxArticles { get; set; } = 20;
}

/// <summary>
/// AI response for a curated cluster insight
/// </summary>
public class CuratedItemInsightDto
{
    [JsonPropertyName("key_fact")]
    public string? KeyFact { get; set; }

    [JsonPropertyName("why_it_matters")]
    public string? WhyItMatters { get; set; }

    [JsonPropertyName("significance")]
    public int Significance { get; set; }
}

/// <summary>
/// Summary of deduplication results
/// </summary>
public class DeduplicationStatsDto
{
    public int OriginalCount { get; set; }

    public int UniqueCount { get; set; }

    public int DuplicatesRemoved { get; set; }
}

/// <summary>
/// Curated intelligence item for clustered articles
/// </summary>
public class CuratedItemDto
{
    public string Title { get; set; } = string.Empty;

    public string KeyFact { get; set; } = string.Empty;

    public string WhyItMatters { get; set; } = string.Empty;

    public int Significance { get; set; }

    public int SourceCount { get; set; }

    public List<string> Sources { get; set; } = new();

    public List<string> ClusterKeywords { get; set; } = new();
}

/// <summary>
/// AI-curated intelligence results
/// </summary>
public class CuratedIntelligenceDto
{
    public string CombinedSummary { get; set; } = string.Empty;

    public string HeadlineInsight { get; set; } = string.Empty;

    public List<CuratedItemDto> CuratedItems { get; set; } = new();

    public DeduplicationStatsDto DeduplicationStats { get; set; } = new();
}
