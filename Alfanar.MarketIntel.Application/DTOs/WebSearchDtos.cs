namespace Alfanar.MarketIntel.Application.DTOs;

/// <summary>
/// DTO for web search requestsAPI endpoint: POST /api/web-search/search
/// </summary>
public class WebSearchRequestDto
{
    public string Keyword { get; set; } = string.Empty;

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public int MaxResults { get; set; } = 10;

    public string SearchProvider { get; set; } = "newsapi";
}

/// <summary>
/// DTO for individual web search results
/// </summary>
public class WebSearchResultDto
{
    public Guid Id { get; set; }

    public string Keyword { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Snippet { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public DateTime? PublishedDate { get; set; }

    public string Source { get; set; } = string.Empty;

    public DateTime RetrievedUtc { get; set; }

    public bool IsFromMonitoring { get; set; }
}

/// <summary>
/// DTO for keyword monitoring configuration
/// </summary>
public class KeywordMonitorDto
{
    public Guid Id { get; set; }

    public string Keyword { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public int CheckIntervalMinutes { get; set; } = 60;

    public DateTime? LastCheckedUtc { get; set; }

    public List<string> Tags { get; set; } = new();

    public int MaxResultsPerCheck { get; set; } = 10;
}

/// <summary>
/// DTO for creating/updating keyword monitors
/// </summary>
public class CreateKeywordMonitorDto
{
    public string Keyword { get; set; } = string.Empty;

    public int CheckIntervalMinutes { get; set; } = 60;

    public List<string> Tags { get; set; } = new();

    public int MaxResultsPerCheck { get; set; } = 10;
}

/// <summary>
/// DTO for technology report generation request
/// </summary>
public class TechnologyReportRequestDto
{
    public string? Title { get; set; }

    public List<string> Keywords { get; set; } = new();

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool IncludeSummary { get; set; } = false;
}

/// <summary>
/// DTO for technology report response
/// </summary>
public class TechnologyReportDto
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public List<string> Keywords { get; set; } = new();

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public DateTime GeneratedUtc { get; set; }

    public string? PdfUrl { get; set; }

    public int TotalResults { get; set; }

    public List<WebSearchResultDto> Results { get; set; } = new();

    public string? Summary { get; set; }
}

/// <summary>
/// Paginated response wrapper
/// </summary>
public class PagedResultDto<T>
{
    public List<T> Items { get; set; } = new();

    public int TotalCount { get; set; }

    public int PageNumber { get; set; }

    public int PageSize { get; set; }

    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;

    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => PageNumber < TotalPages;
}
