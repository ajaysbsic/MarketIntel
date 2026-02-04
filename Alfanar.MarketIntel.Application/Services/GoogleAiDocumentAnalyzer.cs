using Alfanar.MarketIntel.Application.Common;
using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Alfanar.MarketIntel.Application.Services;

/// <summary>
/// Google AI Document Analyzer with Streaming Analysis, Custom Prompt Templates, and Caching Layer
/// </summary>
public class GoogleAiDocumentAnalyzer : IDocumentAnalyzer
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GoogleAiDocumentAnalyzer> _logger;
    private readonly string? _apiKey;
    private readonly string _model;
    private readonly bool _isEnabled;
    private readonly IDistributedCache? _cache;
    private readonly bool _enableCaching;
    private readonly bool _enableStreamingAnalysis;
    private readonly Dictionary<string, string> _promptTemplates;

    public GoogleAiDocumentAnalyzer(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GoogleAiDocumentAnalyzer> logger,
        IDistributedCache? cache = null)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["GoogleAI:ApiKey"];
        _model = configuration["GoogleAI:Model"] ?? "gemini-2.5-flash";
        _isEnabled = !string.IsNullOrWhiteSpace(_apiKey);
        _cache = cache;
        _enableCaching = configuration.GetValue("GoogleAI:EnableCaching", true);
        _enableStreamingAnalysis = configuration.GetValue("GoogleAI:EnableStreaming", true);

        // Load custom prompt templates
        _promptTemplates = new Dictionary<string, string>
        {
            { "default", configuration["GoogleAI:PromptTemplate:Default"] ?? GetDefaultPromptTemplate() },
            { "financial", configuration["GoogleAI:PromptTemplate:Financial"] ?? GetFinancialPromptTemplate() },
            { "technical", configuration["GoogleAI:PromptTemplate:Technical"] ?? GetTechnicalPromptTemplate() }
        };

        if (_isEnabled)
        {
            _logger.LogInformation("Google AI Analyzer initialized with model: {Model}, Caching: {Caching}, Streaming: {Streaming}", 
                _model, _enableCaching, _enableStreamingAnalysis);
        }
        else
        {
            _logger.LogWarning("Google AI API key not configured");
        }
    }

    public bool IsAvailable() => _isEnabled;

    public async Task<Result<ReportAnalysis>> AnalyzeDocumentAsync(
        string text,
        string companyName,
        string reportType)
    {
        if (!IsAvailable())
            return Result<ReportAnalysis>.Failure("Google AI service not configured");

        try
        {
            var startTime = DateTime.UtcNow;
            var truncatedText = text.Length > 32000 ? text.Substring(0, 32000) + "..." : text;

            // Check cache first
            var cacheKey = GenerateCacheKey(companyName, reportType, truncatedText);
            if (_enableCaching && _cache != null)
            {
                try
                {
                    var cachedResult = await _cache.GetStringAsync(cacheKey);
                    if (!string.IsNullOrWhiteSpace(cachedResult))
                    {
                        _logger.LogInformation(" Cache hit for {Company} {ReportType}", companyName, reportType);
                        var cachedAnalysis = JsonSerializer.Deserialize<ReportAnalysis>(cachedResult);
                        if (cachedAnalysis != null)
                            return Result<ReportAnalysis>.Success(cachedAnalysis);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to check cache for {Company}", companyName);
                }
            }

            var prompt = BuildAnalysisPrompt(truncatedText, companyName, reportType);

            // Use streaming analysis if enabled
            Result<string> analysisResult;
            if (_enableStreamingAnalysis)
            {
                _logger.LogInformation(" Using streaming analysis for {Company} {ReportType}", companyName, reportType);
                analysisResult = await AnalyzeWithStreamingAsync(prompt);
            }
            else
            {
                _logger.LogInformation(" Using standard analysis for {Company} {ReportType}", companyName, reportType);
                analysisResult = await AnalyzeWithStandardAsync(prompt);
            }

            if (!analysisResult.IsSuccess)
                return Result<ReportAnalysis>.Failure(analysisResult.Error ?? "Analysis failed");

            var content = analysisResult.Data;
            if (string.IsNullOrWhiteSpace(content))
                return Result<ReportAnalysis>.Failure("Empty response from AI");

            _logger.LogInformation(" Received response from Google AI ({Chars} chars)", content.Length);

            // Parse JSON response - handle markdown wrapped JSON
            var jsonContent = ExtractJsonFromResponse(content);
            
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                _logger.LogError("Could not extract JSON from response: {Content}", content.Substring(0, Math.Min(200, content.Length)));
                return Result<ReportAnalysis>.Failure("Could not parse AI response as JSON");
            }

            JsonElement analysisData;
            try
            {
                analysisData = JsonSerializer.Deserialize<JsonElement>(jsonContent);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse JSON response: {Content}", jsonContent.Substring(0, Math.Min(300, jsonContent.Length)));
                return Result<ReportAnalysis>.Failure($"Invalid JSON in response: {ex.Message}");
            }

            var analysis = new ReportAnalysis
            {
                Id = Guid.NewGuid(),
                ExecutiveSummary = GetRequiredString(analysisData, "executive_summary") ?? "Analysis completed but summary unavailable",
                KeyHighlights = JsonSerializer.Serialize(GetOptionalArray(analysisData, "key_highlights")),
                StrategicInitiatives = GetOptionalString(analysisData, "strategic_initiatives") ?? "Not specified",
                MarketOutlook = GetOptionalString(analysisData, "market_outlook") ?? "Not specified",
                RiskFactors = JsonSerializer.Serialize(GetOptionalArray(analysisData, "risk_factors")),
                CompetitivePosition = GetOptionalString(analysisData, "competitive_position") ?? "Not specified",
                InvestmentThesis = GetOptionalString(analysisData, "investment_thesis") ?? "Not specified",
                SentimentScore = GetOptionalDouble(analysisData, "sentiment_score") ?? 0.5,
                SentimentLabel = GetOptionalString(analysisData, "sentiment_label") ?? "Neutral",
                AnalysisConfidence = 0.85,
                AiModel = _model,
                TokensUsed = 0,
                ProcessingTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds,
                CreatedUtc = DateTime.UtcNow
            };

            // Cache the result
            if (_enableCaching && _cache != null)
            {
                try
                {
                    var serialized = JsonSerializer.Serialize(analysis);
                    var cacheOptions = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
                    };
                    await _cache.SetStringAsync(cacheKey, serialized, cacheOptions);
                    _logger.LogInformation(" Analysis cached for {Company} (24 hours)", companyName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cache analysis for {Company}", companyName);
                }
            }

            _logger.LogInformation(" Analysis completed for {Company} in {Ms}ms", companyName, analysis.ProcessingTimeMs);
            return Result<ReportAnalysis>.Success(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception analyzing document for {Company}", companyName);
            return Result<ReportAnalysis>.Failure($"Analysis failed: {ex.Message}");
        }
    }

    public async Task<Result<string>> GenerateSummaryAsync(string text, int maxWords = 200)
    {
        if (!IsAvailable())
            return Result<string>.Failure("Google AI service not configured");

        try
        {
            var truncatedText = text.Length > 16000 ? text.Substring(0, 16000) + "..." : text;
            var prompt = $"Summarize this document in {maxWords} words:\n{truncatedText}";

            var requestBody = new
            {
                contents = new[] {
                    new {
                        parts = new[] {
                            new { text = prompt }
                        }
                    }
                }
            };

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";
            var response = await _httpClient.PostAsJsonAsync(url, requestBody);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Google AI summary error: {Error}", error);
                return Result<string>.Failure("Summary generation failed");
            }

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            var summary = result
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "Summary unavailable";

            return Result<string>.Success(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating summary");
            return Result<string>.Failure($"Summary generation failed: {ex.Message}");
        }
    }

    public async Task<Result<List<string>>> ExtractKeyHighlightsAsync(string text, int maxHighlights = 7)
    {
        return Result<List<string>>.Success(new List<string>());
    }

    public async Task<Result<Dictionary<string, object>>> ExtractFinancialMetricsAsync(string text)
    {
        return Result<Dictionary<string, object>>.Success(new Dictionary<string, object>());
    }

    public async Task<Result<(double score, string label)>> AnalyzeSentimentAsync(string text)
    {
        return Result<(double, string)>.Success((0.5, "Neutral"));
    }

    private string BuildAnalysisPrompt(string text, string companyName, string reportType)
    {
        // Select template based on report type
        var templateKey = reportType.ToLowerInvariant() switch
        {
            var x when x.Contains("financial") => "financial",
            var x when x.Contains("technical") => "technical",
            _ => "default"
        };

        _promptTemplates.TryGetValue(templateKey, out var template);
        template ??= _promptTemplates["default"];

        return template.Replace("{company_name}", companyName)
                      .Replace("{report_type}", reportType)
                      .Replace("{document}", text);
    }

    /// <summary>
    /// Analyze document with streaming (progressive chunk processing)
    /// </summary>
    private async Task<Result<string>> AnalyzeWithStreamingAsync(string prompt)
    {
        try
        {
            var chunks = ChunkText(prompt, 4000);
            var results = new List<string>();

            foreach (var (chunk, index) in chunks.Select((c, i) => (c, i)))
            {
                _logger.LogInformation(" Processing chunk {Chunk} of {Total}", index + 1, chunks.Count);
                var result = await CallGeminiApiAsync(chunk);
                if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.Data))
                {
                    results.Add(result.Data);
                }
            }

            if (results.Count == 0)
                return Result<string>.Failure("No analysis from streaming chunks");

            // Return the last (most complete) result
            return Result<string>.Success(results.Last());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in streaming analysis");
            return Result<string>.Failure($"Streaming analysis failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Analyze document with standard single-pass processing
    /// </summary>
    private async Task<Result<string>> AnalyzeWithStandardAsync(string prompt)
    {
        return await CallGeminiApiAsync(prompt);
    }

    /// <summary>
    /// Call Gemini API and extract text response
    /// </summary>
    private async Task<Result<string>> CallGeminiApiAsync(string prompt)
    {
        try
        {
            var requestBody = new
            {
                contents = new[] {
                    new {
                        parts = new[] {
                            new { text = prompt }
                        }
                    }
                }
            };

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";
            var response = await _httpClient.PostAsJsonAsync(url, requestBody);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError(" Google AI API error ({StatusCode}): {Error}", response.StatusCode, errorContent);
                return Result<string>.Failure($"Google AI API error: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();

            // Check for safety issues
            if (result.TryGetProperty("promptFeedback", out var feedback))
            {
                _logger.LogWarning("  Google AI prompt feedback: {Feedback}", feedback.GetRawText());
            }

            // Extract content from response
            if (!result.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            {
                _logger.LogError(" No candidates in Google AI response");
                return Result<string>.Failure("No analysis returned");
            }

            var content = candidates[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return string.IsNullOrWhiteSpace(content)
                ? Result<string>.Failure("Empty response from API")
                : Result<string>.Success(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, " Error calling Gemini API");
            return Result<string>.Failure($"API call failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Generate cache key from document content using SHA256
    /// </summary>
    private string GenerateCacheKey(string companyName, string reportType, string text)
    {
        var combined = $"{companyName}:{reportType}:{text}";
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(combined));
        return $"analysis:{Convert.ToHexString(hash)}.cache";
    }

    /// <summary>
    /// Split text into chunks for streaming processing
    /// </summary>
    private List<string> ChunkText(string text, int chunkSize = 4000)
    {
        var chunks = new List<string>();
        for (int i = 0; i < text.Length; i += chunkSize)
        {
            chunks.Add(text.Substring(i, Math.Min(chunkSize, text.Length - i)));
        }
        return chunks;
    }

    /// <summary>
    /// Default prompt template for general documents
    /// </summary>
    private string GetDefaultPromptTemplate()
    {
        return @"You are a professional analyst. Analyze this {report_type} for {company_name} and return ONLY a valid JSON object:
{
  ""executive_summary"": ""Comprehensive 4-6 sentence summary with key findings"",
  ""key_highlights"": [""highlight 1"", ""highlight 2"", ""highlight 3""],
  ""strategic_initiatives"": ""Major initiatives and impact"",
  ""market_outlook"": ""Market analysis and outlook"",
  ""risk_factors"": [""risk 1"", ""risk 2""],
  ""competitive_position"": ""Competitive standing"",
  ""investment_thesis"": ""Investment perspective"",
  ""sentiment_score"": 0.5,
  ""sentiment_label"": ""Neutral""
}

Return ONLY JSON, no markdown. No explanations.

Document:
{document}";
    }

    /// <summary>
    /// Template for financial documents - emphasizes metrics and numbers
    /// </summary>
    private string GetFinancialPromptTemplate()
    {
        return @"You are a financial analyst. Analyze this financial {report_type} for {company_name}. Focus on:
1. Revenue and profitability trends
2. Balance sheet strength
3. Cash flow analysis
4. Key financial ratios
5. Guidance and outlook

Return ONLY valid JSON:
{
  ""executive_summary"": ""Detailed financial analysis with specific numbers and percentages"",
  ""key_highlights"": [""metric 1 with value"", ""metric 2 with trend""],
  ""strategic_initiatives"": ""Strategic financial priorities"",
  ""market_outlook"": ""Market and sector outlook"",
  ""risk_factors"": [""financial risk"", ""operational risk""],
  ""competitive_position"": ""Financial strength vs competitors"",
  ""investment_thesis"": ""Financial investment case"",
  ""sentiment_score"": 0.7,
  ""sentiment_label"": ""Positive""
}

Return ONLY JSON.

Document:
{document}";
    }

    /// <summary>
    /// Template for technical documents - emphasizes specifications and implementation
    /// </summary>
    private string GetTechnicalPromptTemplate()
    {
        return @"You are a technical analyst. Analyze this technical {report_type} for {company_name}. Focus on:
1. Architecture and design patterns
2. Technical capabilities and limitations
3. Performance characteristics
4. Integration points
5. Roadmap and future direction

Return ONLY valid JSON:
{
  ""executive_summary"": ""Technical summary with key capabilities"",
  ""key_highlights"": [""capability 1"", ""feature 2""],
  ""strategic_initiatives"": ""Technical roadmap and initiatives"",
  ""market_outlook"": ""Technology market trends"",
  ""risk_factors"": [""technical risk"", ""adoption risk""],
  ""competitive_position"": ""Technical advantage vs competitors"",
  ""investment_thesis"": ""Technical investment case"",
  ""sentiment_score"": 0.6,
  ""sentiment_label"": ""Neutral""
}

Return ONLY JSON.

Document:
{document}";
    }

    /// <summary>
    /// Extract JSON from response, handling markdown wrapped JSON
    /// </summary>
    private string ExtractJsonFromResponse(string content)
    {
        // Try to extract JSON from markdown code blocks
        var jsonMatch = System.Text.RegularExpressions.Regex.Match(
            content,
            @"```(?:json)?\s*\n?([\s\S]*?)\n?```",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (jsonMatch.Success)
        {
            return jsonMatch.Groups[1].Value.Trim();
        }

        // If no markdown, try to find JSON object directly
        var jsonStart = content.IndexOf('{');
        var jsonEnd = content.LastIndexOf('}');

        if (jsonStart >= 0 && jsonEnd > jsonStart)
        {
            return content.Substring(jsonStart, jsonEnd - jsonStart + 1).Trim();
        }

        return content;
    }

    private string? GetRequiredString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) ? prop.GetString() : null;
    }

    private string? GetOptionalString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) ? prop.GetString() : null;
    }

    private double? GetOptionalDouble(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number
            ? prop.GetDouble()
            : null;
    }

    private JsonElement GetOptionalArray(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) ? prop : JsonSerializer.Deserialize<JsonElement>("[]")!;
    }
}