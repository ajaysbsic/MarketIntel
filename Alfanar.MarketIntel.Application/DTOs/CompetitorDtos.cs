namespace Alfanar.MarketIntel.Application.DTOs;

public class CompetitorDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Industry { get; set; } = string.Empty;

    public string Region { get; set; } = string.Empty;

    public List<string> Keywords { get; set; } = new();

    public string? Website { get; set; }

    public bool IsActive { get; set; }

    public bool IsAutoDetected { get; set; }

    public DateTime CreatedUtc { get; set; }

    public string? CreatedBy { get; set; }

    public string? Notes { get; set; }
}

public class CreateCompetitorDto
{
    public string Name { get; set; } = string.Empty;

    public string Industry { get; set; } = string.Empty;

    public string Region { get; set; } = string.Empty;

    public List<string> Keywords { get; set; } = new();

    public string? Website { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Notes { get; set; }
}

public class CompetitorMentionDto
{
    public Guid Id { get; set; }

    public Guid CompetitorId { get; set; }

    public string CompetitorName { get; set; } = string.Empty;

    public string SourceType { get; set; } = string.Empty;

    public Guid SourceId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Snippet { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public double? SentimentScore { get; set; }

    public string? SentimentLabel { get; set; }

    public string MentionContext { get; set; } = string.Empty;

    public DateTime DetectedUtc { get; set; }

    public bool IsAutoDetected { get; set; }
}

public class CompetitorMentionTrendPointDto
{
    public DateTime WeekStart { get; set; }

    public int Count { get; set; }
}

public class CompetitorDashboardDto
{
    public CompetitorDto Competitor { get; set; } = new();

    public int TotalMentions { get; set; }

    public int Last30DaysMentions { get; set; }

    public double AverageSentiment { get; set; }

    public List<string> TopContextTypes { get; set; } = new();

    public List<CompetitorMentionTrendPointDto> MentionTrend { get; set; } = new();

    public List<CompetitorMentionDto> RecentMentions { get; set; } = new();
}

public class CompetitorComparisonItemDto
{
    public Guid CompetitorId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int TotalMentions { get; set; }

    public int Last30DaysMentions { get; set; }

    public double AverageSentiment { get; set; }
}

public class CompetitorComparisonDto
{
    public List<CompetitorComparisonItemDto> Items { get; set; } = new();
}

public class CompetitorScanRequestDto
{
    public string SourceType { get; set; } = string.Empty;

    public Guid SourceId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Snippet { get; set; }

    public string? BodyText { get; set; }

    public string? Url { get; set; }
}
