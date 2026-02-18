using Alfanar.MarketIntel.Application.DTOs;
using Alfanar.MarketIntel.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Alfanar.MarketIntel.Application.Services;

/// <summary>
/// AI Chat Service with RAG Integration
/// Provides intelligent conversations enhanced with database context
/// </summary>
public interface IAiChatService
{
    Task<AiResponseDto> GetAiResponseAsync(ChatRequestDto request, bool enableLiveWebSearch = true);
    Task<List<string>> GenerateRelatedQueriesAsync(string query, string response);
}

public class AiChatService : IAiChatService
{
    private readonly IRagContextService _ragContextService;
    private readonly IDocumentAnalyzer _documentAnalyzer;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiChatService> _logger;
    private readonly bool _enableLiveWebSearch;

    // System prompt that instructs the AI how to behave
    private const string SYSTEM_PROMPT = @"
You are an expert financial analyst and market intelligence specialist for Alfanar Market Intelligence.

Your role:
1. Answer questions using BOTH our database AND live web search results
2. **IMPORTANT**: When you see ""LATEST WEB SEARCH RESULTS"" in the context, these are LIVE INTERNET RESULTS retrieved today
3. You CAN and SHOULD use live web search results to answer questions about current events, tenders, acquisitions, or recent news
4. Always cite your sources (database sources OR web URLs)
5. Be specific with numbers, percentages, and financial metrics
6. Clearly indicate your confidence level (high, medium, low)
7. Suggest related queries for follow-up analysis
8. Highlight risks, opportunities, and trends
9. Keep responses concise but informative (2-3 paragraphs max)

When providing answers:
- If live web search results are available, USE THEM to provide current information
- Lead with the most important finding
- Support with specific data points and URLs when available
- Mention dates of information
- List sources used (including web URLs from search results)
- Note any limitations or assumptions

**Search Capability**:
- If the user explicitly asks to ""search"", ""look up"", or ""find on the internet"", prioritize WEB SEARCH RESULTS over database
- Web search results are marked as ""LATEST WEB SEARCH RESULTS (Updated today)"" in context
- These results come from live Google searches and NewsAPI - they are RELIABLE and CURRENT

If insufficient data exists in BOTH database and web search, explicitly state this and suggest what data would help.
";

    public AiChatService(
        IRagContextService ragContextService,
        IDocumentAnalyzer documentAnalyzer,
        IConfiguration configuration,
        ILogger<AiChatService> logger)
    {
        _ragContextService = ragContextService;
        _documentAnalyzer = documentAnalyzer;
        _configuration = configuration;
        _logger = logger;
        _enableLiveWebSearch = _configuration.GetValue("AiChat:EnableLiveWebSearch", true);
    }

    /// <summary>
    /// Main method: Get AI response with RAG context
    /// Flow: Get Context → Build Prompt → Call AI → Parse Response → Return
    /// </summary>
    public async Task<AiResponseDto> GetAiResponseAsync(ChatRequestDto request, bool enableLiveWebSearch = true)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew();
        var response = new AiResponseDto { Timestamp = DateTime.UtcNow };

        try
        {
            _logger.LogInformation($"Processing AI query: {request.Message}");

            // Step 1: Get enriched context from database
            var context = await _ragContextService.GetEnrichedContextWithWebSearchAsync(
                request.Message,
                request.ContextEntity,
                includeWebSearch: _enableLiveWebSearch && enableLiveWebSearch);

            // Step 2: Build the prompt with context
            var prompt = BuildEnhancedPrompt(request, context);

            // Step 3: Call Gemini API
            var aiResponse = await CallGeminiAsync(prompt);

            response.Answer = aiResponse;

            // Step 4: Extract citations from context
            response.Citations = ExtractCitations(context);

            // Step 5: Generate related queries
            response.RelatedQueries = await GenerateRelatedQueriesAsync(request.Message, aiResponse);

            // Step 6: Estimate confidence based on data quality
            response.Confidence = CalculateConfidence(context);

            timer.Stop();
            response.ExecutionTimeMs = timer.ElapsedMilliseconds;

            _logger.LogInformation($"AI response generated in {timer.ElapsedMilliseconds}ms");

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI response");
            response.Answer = $"I encountered an error processing your request: {ex.Message}";
            timer.Stop();
            response.ExecutionTimeMs = timer.ElapsedMilliseconds;
            return response;
        }
    }

    /// <summary>
    /// Build enhanced prompt with context from database
    /// This is the key to RAG - augmenting the prompt with relevant data
    /// </summary>
    private string BuildEnhancedPrompt(ChatRequestDto request, RagContextDto context)
    {
        var sb = new StringBuilder();

        sb.AppendLine("=== CONTEXT FROM DATABASE AND LIVE WEB ===");
        sb.AppendLine($"Current System Date: {context.CurrentDate:MMMM dd, yyyy}");
        sb.AppendLine($"Query Timestamp: {DateTime.UtcNow:MMMM dd, yyyy HH:mm:ss UTC}");
        sb.AppendLine();

        // Detect if user is explicitly requesting web search
        var requestsWebSearch = Regex.IsMatch(request.Message, 
            @"\b(search|look\s+up|find|internet|web|online|google|tell\s+me\s+about|any\s+news|recent|latest|tenders?|acquisitions?)\b", 
            RegexOptions.IgnoreCase);

        if (requestsWebSearch && context.WebSearchResults.Count > 0)
        {
            sb.AppendLine("🌐 === LIVE WEB SEARCH RESULTS (Retrieved from Internet TODAY) ===");
            sb.AppendLine("NOTE: These are REAL, CURRENT results from Google Custom Search and NewsAPI.");
            sb.AppendLine("Use these results to answer the user's question about current events.\n");
            
            foreach (var result in context.WebSearchResults.Take(8))
            {
                sb.AppendLine($"🔹 {result.Title}");
                sb.AppendLine($"   🔗 URL: {result.Url}");
                sb.AppendLine($"   📅 Retrieved: {result.RetrievedAt:MMMM dd, yyyy HH:mm UTC}");
                sb.AppendLine($"   📝 Snippet: {result.Snippet}");
                if (!string.IsNullOrEmpty(result.Source))
                {
                    sb.AppendLine($"   🏢 Source: {result.Source}");
                }
                sb.AppendLine();
            }
            sb.AppendLine("=== END OF LIVE WEB RESULTS ===\n");
        }
        else if (context.WebSearchResults.Count > 0)
        {
            sb.AppendLine("LATEST WEB SEARCH RESULTS (Updated today):");
            foreach (var result in context.WebSearchResults.Take(5))
            {
                sb.AppendLine($"• {result.Title}");
                sb.AppendLine($"  URL: {result.Url}");
                sb.AppendLine($"  Summary: {result.Snippet}");
                sb.AppendLine();
            }
        }

        // Add reports context
        if (context.Reports.Count > 0)
        {
            sb.AppendLine("RECENT FINANCIAL REPORTS:");
            foreach (var report in context.Reports.Take(3))
            {
                sb.AppendLine($"• {report.Title}");
                sb.AppendLine($"  Company: {report.CompanyName}");
                sb.AppendLine($"  Published: {report.PublishedDate:MMMM dd, yyyy}");
                sb.AppendLine($"  Summary: {report.Summary}");
                sb.AppendLine();
            }
        }

        // Add news context
        if (context.NewsArticles.Count > 0)
        {
            sb.AppendLine("RECENT NEWS (Last 30 Days):");
            foreach (var news in context.NewsArticles.Take(3))
            {
                sb.AppendLine($"• {news.Title}");
                sb.AppendLine($"  Source: {news.Source}");
                sb.AppendLine($"  Published: {news.PublishedDate:MMMM dd, yyyy}");
                sb.AppendLine($"  Summary: {news.Summary}");
                sb.AppendLine();
            }
        }

        // Add alerts context
        if (context.Alerts.Count > 0)
        {
            sb.AppendLine("ACTIVE ALERTS:");
            foreach (var alert in context.Alerts.Take(3))
            {
                sb.AppendLine($"• [{alert.Severity}] {alert.Title}");
                sb.AppendLine($"  Type: {alert.AlertType}");
                sb.AppendLine($"  Description: {alert.Description}");
                sb.AppendLine();
            }
        }

        sb.AppendLine("=== USER QUESTION ===");
        sb.AppendLine(request.Message);
        sb.AppendLine();
        
        if (requestsWebSearch && context.WebSearchResults.Count > 0)
        {
            sb.AppendLine("**IMPORTANT INSTRUCTION**: The user is asking for current/live information.");
            sb.AppendLine("You MUST use the LIVE WEB SEARCH RESULTS provided above to answer this query.");
            sb.AppendLine("Include specific URLs, dates, and details from the web search results.");
            sb.AppendLine("Cite your sources by including the URLs from the search results.");
        }
        else
        {
            sb.AppendLine("Based on the above context from our database and live web results (if available), provide a specific, data-driven answer.");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Call document analyzer for AI response generation
    /// Uses the configured LLM (Gemini or OpenAI)
    /// </summary>
    private async Task<string> CallGeminiAsync(string prompt)
    {
        try
        {
            var result = await _documentAnalyzer.GenerateSummaryAsync(prompt, 500);
            if (result.IsSuccess)
            {
                return result.Data;
            }
            return "Unable to generate response";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling AI API");
            throw;
        }
    }

    /// <summary>
    /// Extract citations from retrieved data
    /// Creates source references for AI response
    /// </summary>
    private List<CitationDto> ExtractCitations(RagContextDto context)
    {
        var citations = new List<CitationDto>();

        // Add report citations
        foreach (var report in context.Reports.Take(3))
        {
            citations.Add(new CitationDto
            {
                SourceId = report.Id.ToString(),
                SourceType = "Report",
                Title = report.Title,
                PublishedDate = report.PublishedDate,
                Url = $"/reports/{report.Id}"
            });
        }

        // Add news citations
        foreach (var news in context.NewsArticles.Take(2))
        {
            citations.Add(new CitationDto
            {
                SourceId = news.Id,
                SourceType = "News",
                Title = news.Title,
                PublishedDate = news.PublishedDate,
                Url = news.Summary // Placeholder - would use actual URL
            });
        }

        // Add alert citations
        foreach (var alert in context.Alerts.Take(1))
        {
            citations.Add(new CitationDto
            {
                SourceId = alert.Id,
                SourceType = "Alert",
                Title = alert.Title,
                PublishedDate = alert.UpdatedAt,
                Url = $"/alerts/{alert.Id}"
            });
        }

        return citations;
    }

    /// <summary>
    /// Calculate confidence level based on data quality and quantity
    /// More recent, relevant data = higher confidence
    /// </summary>
    private double CalculateConfidence(RagContextDto context)
    {
        var score = 0.0;

        // Base confidence on number of sources
        if (context.Reports.Count > 0) score += 0.25;
        if (context.NewsArticles.Count > 0) score += 0.25;
        if (context.Alerts.Count > 0) score += 0.1;

        // Bonus for recent data
        if (context.Reports.Any(r => r.PublishedDate > DateTime.UtcNow.AddDays(-7)))
            score += 0.15;
        if (context.NewsArticles.Any(n => n.PublishedDate > DateTime.UtcNow.AddDays(-3)))
            score += 0.15;

        // Bonus for high relevance data
        if (context.Reports.Any(r => r.Relevance > 0.7)) score += 0.1;
        if (context.NewsArticles.Any(n => n.Relevance > 0.7)) score += 0.1;

        return Math.Min(score, 0.99); // Cap at 0.99 (never 100% confident)
    }

    /// <summary>
    /// Generate related follow-up queries for exploration
    /// Helps users discover related analysis opportunities
    /// </summary>
    public async Task<List<string>> GenerateRelatedQueriesAsync(string query, string response)
    {
        var relatedQueries = new List<string>();

        // Extract entities from original query
        var entities = await Task.Run(() => ExtractEntitiesFromText(query));
        
        // Generate related queries based on entities
        foreach (var entity in entities.Take(2))
        {
            relatedQueries.Add($"What are {entity}'s major competitors?");
            relatedQueries.Add($"What risks does {entity} face?");
            relatedQueries.Add($"What is {entity}'s market position?");
        }

        // Add generic follow-ups
        relatedQueries.Add("What's the industry trend?");
        relatedQueries.Add("What should I monitor?");

        return relatedQueries.Distinct().Take(5).ToList();
    }

    /// <summary>
    /// Extract entities (proper nouns) from text
    /// Simple regex-based extraction
    /// </summary>
    private List<string> ExtractEntitiesFromText(string text)
    {
        // Simple pattern: capitalized words of 3+ characters
        var pattern = @"\b[A-Z][a-z]{2,}\b";
        var matches = Regex.Matches(text, pattern);
        return matches.Cast<Match>().Select(m => m.Value).Distinct().Take(3).ToList();
    }
}
