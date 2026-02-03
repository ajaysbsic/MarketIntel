namespace Alfanar.MarketIntel.Application.DTOs;

/// <summary>
/// Complete RAG context for AI queries
/// Contains all relevant data from database for enriched AI responses
/// </summary>
public class RagContextDto
{
    public string Query { get; set; } = string.Empty;
    public string? Entity { get; set; }
    public DateTime CurrentDate { get; set; }
    public DateTime RetrievalTimestamp { get; set; }
    
    public List<ReportContextDto> Reports { get; set; } = new();
    public List<NewsContextDto> NewsArticles { get; set; } = new();
    public List<AlertContextDto> Alerts { get; set; } = new();
    public List<string> RelatedEntities { get; set; } = new();
    
    public string GetFormattedContext()
    {
        var sb = new System.Text.StringBuilder();
        
        sb.AppendLine("=== RAG CONTEXT ===");
        sb.AppendLine($"Current Date: {CurrentDate:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Query: {Query}");
        if (!string.IsNullOrEmpty(Entity))
            sb.AppendLine($"Entity: {Entity}");
        sb.AppendLine();

        if (Reports.Count > 0)
        {
            sb.AppendLine("FINANCIAL REPORTS:");
            foreach (var report in Reports.OrderByDescending(r => r.Relevance))
            {
                sb.AppendLine($"- [{report.Relevance:P}] {report.Title} ({report.PublishedDate:yyyy-MM-dd})");
                sb.AppendLine($"  Company: {report.CompanyName}");
                sb.AppendLine($"  Summary: {report.Summary}");
                sb.AppendLine();
            }
        }

        if (NewsArticles.Count > 0)
        {
            sb.AppendLine("RECENT NEWS (Last 30 days):");
            foreach (var news in NewsArticles.OrderByDescending(n => n.Relevance).Take(5))
            {
                sb.AppendLine($"- [{news.Relevance:P}] {news.Title}");
                sb.AppendLine($"  Source: {news.Source} ({news.PublishedDate:yyyy-MM-dd})");
                sb.AppendLine($"  Summary: {news.Summary}");
                sb.AppendLine();
            }
        }

        if (Alerts.Count > 0)
        {
            sb.AppendLine("ACTIVE ALERTS:");
            foreach (var alert in Alerts.OrderByDescending(a => a.Relevance))
            {
                sb.AppendLine($"- [{alert.Severity}] {alert.Title}");
                sb.AppendLine($"  Description: {alert.Description}");
                sb.AppendLine($"  Type: {alert.AlertType} (Updated: {alert.UpdatedAt:yyyy-MM-dd})");
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }
}

public class ReportContextDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public DateTime PublishedDate { get; set; }
    public double Relevance { get; set; } // 0.0 - 1.0
}

public class NewsContextDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public DateTime PublishedDate { get; set; }
    public string Source { get; set; } = string.Empty;
    public double Relevance { get; set; } // 0.0 - 1.0
}

public class AlertContextDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty; // High, Medium, Low
    public string AlertType { get; set; } = string.Empty; // Alert type (MarginDrop, RevenueGrowth, etc)
    public DateTime UpdatedAt { get; set; }
    public double Relevance { get; set; } // 0.0 - 1.0
}

/// <summary>
/// AI Response with citations and metadata
/// </summary>
public class AiResponseDto
{
    public string Answer { get; set; } = string.Empty;
    public List<CitationDto> Citations { get; set; } = new();
    public double Confidence { get; set; }
    public DateTime Timestamp { get; set; }
    public List<string> RelatedQueries { get; set; } = new();
    public long ExecutionTimeMs { get; set; }
}

public class CitationDto
{
    public string SourceId { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty; // "Report", "News", "Alert"
    public string Title { get; set; } = string.Empty;
    public DateTime PublishedDate { get; set; }
    public string Url { get; set; } = string.Empty;
}

/// <summary>
/// Chat message for conversation history
/// </summary>
public class ChatMessageDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Role { get; set; } = string.Empty; // "user" or "assistant"
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public AiResponseDto? AiResponse { get; set; }
}

/// <summary>
/// Chat request/response wrapper
/// </summary>
public class ChatRequestDto
{
    public string Message { get; set; } = string.Empty;
    public string? SessionId { get; set; }
    public List<ChatMessageDto> ConversationHistory { get; set; } = new();
    public string? ContextEntity { get; set; }
}
