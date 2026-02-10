namespace Alfanar.MarketIntel.Application.DTOs;

public class TechnologyIntelligenceFilterDto
{
    public List<string>? Keywords { get; set; }
    public string? Region { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public List<string>? SourceTypes { get; set; }
}

public class TechnologyOverviewDto
{
    public int TotalItems { get; set; }
    public int NewsCount { get; set; }
    public int ReportCount { get; set; }
    public int DistinctRegions { get; set; }
    public List<string> TopKeywords { get; set; } = new();
}

public class TechnologyTrendPointDto
{
    public DateTime PeriodStart { get; set; }
    public int NewsCount { get; set; }
    public int ReportCount { get; set; }
    public int TotalCount { get; set; }
}

public class TechnologyRegionSignalDto
{
    public string Region { get; set; } = "Global";
    public int NewsCount { get; set; }
    public int ReportCount { get; set; }
    public int TotalCount { get; set; }
}

public class TechnologyKeyPlayerDto
{
    public string Name { get; set; } = default!;
    public string SourceType { get; set; } = default!;
    public int Mentions { get; set; }
}

public class TechnologyInsightDto
{
    public string Title { get; set; } = default!;
    public string Detail { get; set; } = default!;
    public string InsightType { get; set; } = default!;
}

public class TechnologyIntelligenceSummaryDto
{
    public TechnologyOverviewDto Overview { get; set; } = new();
    public List<TechnologyTrendPointDto> Timeline { get; set; } = new();
    public List<TechnologyRegionSignalDto> Regions { get; set; } = new();
    public List<TechnologyKeyPlayerDto> KeyPlayers { get; set; } = new();
    public List<TechnologyInsightDto> Insights { get; set; } = new();
}
