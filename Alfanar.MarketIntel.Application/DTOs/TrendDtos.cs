namespace Alfanar.MarketIntel.Application.DTOs;

public class GenerateSnapshotRequestDto
{
    public DateTime? Date { get; set; }
}

public class TrendSnapshotDto
{
    public string Keyword { get; set; } = string.Empty;

    public DateTime SnapshotDate { get; set; }

    public int MentionCount { get; set; }

    public int NewsCount { get; set; }

    public int WebSearchCount { get; set; }

    public double AverageSentiment { get; set; }

    public int SignalStrength { get; set; }
}

public class TrendPointDto
{
    public DateTime Date { get; set; }

    public int Count { get; set; }

    public double? Sentiment { get; set; }
}

public class CompetitorVisibilityPointDto
{
    public DateTime Date { get; set; }

    public int Count { get; set; }
}

public class NoiseSignalPointDto
{
    public DateTime Date { get; set; }

    public int NoiseCount { get; set; }

    public int SignalCount { get; set; }
}

public class TrendSeriesDto
{
    public string Keyword { get; set; } = string.Empty;

    public List<TrendPointDto> Points { get; set; } = new();
}

public class TrendComparisonDto
{
    public List<TrendSeriesDto> Series { get; set; } = new();
}

public class WeeklyDigestDto
{
    public string Summary { get; set; } = string.Empty;

    public DateTime GeneratedUtc { get; set; }
}
