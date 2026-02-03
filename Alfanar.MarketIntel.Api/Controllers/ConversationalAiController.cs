using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Application.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Text.Json;

namespace Alfanar.MarketIntel.Api.Controllers;

[ApiController]
[Route("api/ai")]
public class ConversationalAiController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConversationalAiController> _logger;
    private readonly INewsService _newsService;
    private readonly IReportService _reportService;

    public ConversationalAiController(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ConversationalAiController> logger,
        INewsService newsService,
        IReportService reportService)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _newsService = newsService;
        _reportService = reportService;
    }

    [HttpPost("query")]
    public async Task<IActionResult> QueryConversationalAI([FromBody] ConversationalAIRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Query))
            return BadRequest(new { error = "Query cannot be empty" });

        try
        {
            _logger.LogInformation("Processing AI query: {Query}", request.Query);

            // Build context from market data
            var contextData = await BuildContextDataAsync(request.Context);
            
            // Build prompt with market context
            var prompt = BuildConversationalPrompt(request.Query, contextData);

            // Call Google AI API
            var apiKey = _configuration["GoogleAI:ApiKey"];
            var model = _configuration["GoogleAI:Model"] ?? "gemini-2.5-flash";

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("Google AI API key not configured");
                return StatusCode(503, new { error = "AI service not available" });
            }

            var requestBody = new
            {
                contents = new[] {
                    new {
                        parts = new[] {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    maxOutputTokens = 1000,
                    temperature = 0.7,
                    topP = 0.95
                }
            };

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

            _logger.LogInformation("Calling Google AI API with model: {Model}", model);

            var response = await _httpClient.PostAsJsonAsync(url, requestBody);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Google AI API error ({StatusCode}): {Error}", response.StatusCode, errorContent);
                return StatusCode((int)response.StatusCode, new { error = "Failed to get AI response" });
            }

            var content = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(content);
            var root = jsonDoc.RootElement;

            // Extract the generated text from Google AI response
            string generatedText = ExtractTextFromResponse(root);

            if (string.IsNullOrWhiteSpace(generatedText))
            {
                _logger.LogWarning("Empty response from Google AI API");
                return Ok(new
                {
                    response = "I apologize, but I couldn't generate a response. Please try rephrasing your question.",
                    confidence = 0.3,
                    relatedData = new object[] { }
                });
            }

            _logger.LogInformation("Successfully generated AI response");

            return Ok(new
            {
                response = generatedText,
                confidence = 0.85,
                relatedData = contextData.TopItems ?? new object[] { }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing AI query: {Query}", request?.Query);
            return StatusCode(500, new { error = "An error occurred processing your query. Please try again." });
        }
    }

    private string BuildConversationalPrompt(string userQuery, ContextData contextData)
    {
        var contextStr = string.Empty;

        if (contextData.RecentArticles?.Any() == true)
        {
            contextStr += "\n## Recent Market News:\n";
            foreach (var article in contextData.RecentArticles.Take(5))
            {
                contextStr += $"- {article}\n";
            }
        }

        if (contextData.RecentReports?.Any() == true)
        {
            contextStr += "\n## Recent Financial Reports:\n";
            foreach (var report in contextData.RecentReports.Take(5))
            {
                contextStr += $"- {report}\n";
            }
        }

        if (!string.IsNullOrEmpty(contextData.TopSectors))
        {
            contextStr += $"\n## Active Sectors: {contextData.TopSectors}";
        }

        var prompt = $@"You are a professional market intelligence assistant specializing in financial analysis and market trends. 
You have access to the following market data:
{contextStr}

User Query: {userQuery}

Provide a concise, professional response (2-4 sentences maximum) that:
1. Directly addresses the user's question
2. References relevant market data when applicable
3. Uses market terminology appropriately
4. Provides actionable insights when possible

Keep your response focused, data-driven, and professional.";

        return prompt;
    }

    private async Task<ContextData> BuildContextDataAsync(object? context)
    {
        var contextData = new ContextData();

        try
        {
            // Get recent news
            var newsResult = await _newsService.GetRecentArticlesAsync(count: 5);
            if (newsResult.IsSuccess && newsResult.Data?.Any() == true)
            {
                contextData.RecentArticles = newsResult.Data
                    .Select(n => $"{n.Title} (Source: {n.Source})")
                    .ToList();
            }

            // Get recent reports
            var reportsResult = await _reportService.GetRecentReportsAsync(count: 5);
            if (reportsResult.IsSuccess && reportsResult.Data?.Any() == true)
            {
                contextData.RecentReports = reportsResult.Data
                    .Select(r => $"{r.CompanyName} - {r.Title}")
                    .ToList();
            }

            // Get top sectors/companies
            var companiesResult = await _reportService.GetDistinctCompaniesAsync();
            if (companiesResult.IsSuccess && companiesResult.Data?.Any() == true)
            {
                contextData.TopSectors = string.Join(", ", companiesResult.Data.Take(5));
            }

            // If specific context provided, extract top items for response
            if (context is System.Collections.IEnumerable enumerable)
            {
                contextData.TopItems = enumerable.Cast<object>().Take(3).ToArray();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error building context data for AI query");
        }

        return contextData;
    }

    private string ExtractTextFromResponse(JsonElement root)
    {
        try
        {
            // Navigate: candidates[0].content.parts[0].text
            if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var firstCandidate = candidates[0];
                if (firstCandidate.TryGetProperty("content", out var content) &&
                    content.TryGetProperty("parts", out var parts) &&
                    parts.GetArrayLength() > 0)
                {
                    var firstPart = parts[0];
                    if (firstPart.TryGetProperty("text", out var text))
                    {
                        return text.GetString() ?? string.Empty;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from Google AI response");
        }

        return string.Empty;
    }

    public class ConversationalAIRequest
    {
        public string Query { get; set; } = string.Empty;
        public object? Context { get; set; }
    }

    private class ContextData
    {
        public List<string> RecentArticles { get; set; } = new();
        public List<string> RecentReports { get; set; } = new();
        public string TopSectors { get; set; } = string.Empty;
        public object[] TopItems { get; set; } = Array.Empty<object>();
    }
}
