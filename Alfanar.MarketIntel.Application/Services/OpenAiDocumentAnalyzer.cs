using Alfanar.MarketIntel.Application.Common;
using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Alfanar.MarketIntel.Application.Services;

/// <summary>
/// OpenAI-based document analyzer for financial reports
/// </summary>
public class OpenAiDocumentAnalyzer : IDocumentAnalyzer
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAiDocumentAnalyzer> _logger;
    private readonly string? _apiKey;
    private readonly string _model;
    private readonly int _maxTokens;
    private readonly double _temperature;
    private readonly int _timeoutSeconds;
    private readonly bool _isEnabled;

    public OpenAiDocumentAnalyzer(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OpenAiDocumentAnalyzer> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _apiKey = configuration["OpenAI:ApiKey"];
        _model = configuration["OpenAI:Model"] ?? "gpt-4o-mini";
        _maxTokens = int.Parse(configuration["OpenAI:MaxTokens"] ?? "1500");
        _temperature = double.Parse(configuration["OpenAI:Temperature"] ?? "0.3");
        _timeoutSeconds = int.Parse(configuration["OpenAI:TimeoutSeconds"] ?? "30");
        _isEnabled = bool.Parse(configuration["OpenAI:EnableAiCategorization"] ?? "false");

        // If API key not provided in config, try environment variables
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            var envKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (!string.IsNullOrWhiteSpace(envKey))
            {
                _apiKey = envKey;
                _logger.LogInformation("Using OpenAI API key from environment variable OPENAI_API_KEY");
            }
            else
            {
                var altEnv = Environment.GetEnvironmentVariable("OpenAI__ApiKey");
                if (!string.IsNullOrWhiteSpace(altEnv))
                {
                    _apiKey = altEnv;
                    _logger.LogInformation("Using OpenAI API key from environment variable OpenAI__ApiKey");
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
            // Remove any existing Authorization header then set
            if (_httpClient.DefaultRequestHeaders.Contains("Authorization"))
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            _httpClient.Timeout = TimeSpan.FromSeconds(_timeoutSeconds);
            _logger.LogInformation("OpenAI HttpClient configured");
        }
    }

    public bool IsAvailable()
    {
        return _isEnabled && !string.IsNullOrWhiteSpace(_apiKey);
    }

    public async Task<Result<ReportAnalysis>> AnalyzeDocumentAsync(
        string text,
        string companyName,
        string reportType)
    {
        if (!IsAvailable())
        {
            return Result<ReportAnalysis>.Failure("OpenAI service is not available or not configured");
        }

        try
        {
            var startTime = DateTime.UtcNow;

            // Truncate text if too long (keep first ~32k characters for ~8k tokens)
            var truncatedText = text.Length > 32000 ? text.Substring(0, 32000) + "..." : text;

            var prompt = BuildAnalysisPrompt(truncatedText, companyName, reportType);

            var response = await CallOpenAiAsync(prompt, _maxTokens);

            if (!response.IsSuccess)
                return Result<ReportAnalysis>.Failure(response.Error ?? "Analysis failed");

            var analysisData = response.Data!;
            var processingTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

            // Create ReportAnalysis entity
            var analysis = new ReportAnalysis
            {
                Id = Guid.NewGuid(),
                ExecutiveSummary = analysisData.GetProperty("executive_summary").GetString() ?? "",
                KeyHighlights = JsonSerializer.Serialize(analysisData.GetProperty("key_highlights")),
                StrategicInitiatives = GetOptionalString(analysisData, "strategic_initiatives"),
                MarketOutlook = GetOptionalString(analysisData, "market_outlook"),
                RiskFactors = JsonSerializer.Serialize(GetOptionalArray(analysisData, "risk_factors")),
                CompetitivePosition = GetOptionalString(analysisData, "competitive_position"),
                InvestmentThesis = GetOptionalString(analysisData, "investment_thesis"),
                SentimentScore = GetOptionalDouble(analysisData, "sentiment_score"),
                SentimentLabel = GetOptionalString(analysisData, "sentiment_label") ?? "Neutral",
                AnalysisConfidence = 0.85, // Default confidence
                AiModel = _model,
                TokensUsed = GetOptionalInt(analysisData, "tokens_used"),
                ProcessingTimeMs = processingTime,
                CreatedUtc = DateTime.UtcNow
            };

            // Extract financial metrics
            if (analysisData.TryGetProperty("financial_metrics", out var metrics))
            {
                analysis.FinancialMetrics = metrics.GetRawText();
            }

            // Extract tags
            if (analysisData.TryGetProperty("tags", out var tags))
            {
                analysis.Tags = tags.GetRawText();
            }

            // Extract related entities
            if (analysisData.TryGetProperty("related_entities", out var entities))
            {
                analysis.RelatedEntities = entities.GetRawText();
            }

            _logger.LogInformation(
                "Analysis completed for {Company} {ReportType} in {Ms}ms",
                companyName,
                reportType,
                processingTime);

            return Result<ReportAnalysis>.Success(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing document for {Company}", companyName);
            return Result<ReportAnalysis>.Failure($"Analysis failed: {ex.Message}");
        }
    }

    public async Task<Result<string>> GenerateSummaryAsync(string text, int maxWords = 200)
    {
        if (!IsAvailable())
        {
            return Result<string>.Failure("OpenAI service is not available");
        }

        try
        {
            var truncatedText = text.Length > 16000 ? text.Substring(0, 16000) + "..." : text;

            var prompt = $@"Summarize the following financial document in approximately {maxWords} words. 
Focus on the most important financial highlights and key takeaways:

{truncatedText}";

            var response = await CallOpenAiAsync(prompt, 500);

            if (!response.IsSuccess)
                return Result<string>.Failure(response.Error ?? "Summary generation failed");

            var summary = response.Data!.GetProperty("content").GetString() ?? "Summary unavailable";
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
        if (!IsAvailable())
        {
            return Result<List<string>>.Failure("OpenAI service is not available");
        }

        try
        {
            var truncatedText = text.Length > 16000 ? text.Substring(0, 16000) + "..." : text;

            var prompt = $@"Extract {maxHighlights} key highlights from this financial document. 
Return them as a JSON array of strings:

{truncatedText}";

            var response = await CallOpenAiAsync(prompt, 500);

            if (!response.IsSuccess)
                return Result<List<string>>.Failure(response.Error ?? "Highlight extraction failed");

            var content = response.Data!.GetProperty("content").GetString() ?? "[]";
            var highlights = JsonSerializer.Deserialize<List<string>>(content) ?? new List<string>();

            return Result<List<string>>.Success(highlights);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting key highlights");
            return Result<List<string>>.Failure($"Highlight extraction failed: {ex.Message}");
        }
    }

    public async Task<Result<Dictionary<string, object>>> ExtractFinancialMetricsAsync(string text)
    {
        if (!IsAvailable())
        {
            return Result<Dictionary<string, object>>.Failure("OpenAI service is not available");
        }

        try
        {
            var truncatedText = text.Length > 16000 ? text.Substring(0, 16000) + "..." : text;

            var prompt = $@"Extract key financial metrics from this document and return as JSON with keys like 
'revenue', 'growth_rate', 'profit_margin', 'eps', etc. Use numbers where possible:

{truncatedText}";

            var response = await CallOpenAiAsync(prompt, 500);

            if (!response.IsSuccess)
                return Result<Dictionary<string, object>>.Failure(response.Error ?? "Metrics extraction failed");

            var content = response.Data!.GetProperty("content").GetString() ?? "{}";
            var metrics = JsonSerializer.Deserialize<Dictionary<string, object>>(content) 
                ?? new Dictionary<string, object>();

            return Result<Dictionary<string, object>>.Success(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting financial metrics");
            return Result<Dictionary<string, object>>.Failure($"Metrics extraction failed: {ex.Message}");
        }
    }

    public async Task<Result<(double score, string label)>> AnalyzeSentimentAsync(string text)
    {
        if (!IsAvailable())
        {
            return Result<(double, string)>.Failure("OpenAI service is not available");
        }

        try
        {
            var truncatedText = text.Length > 8000 ? text.Substring(0, 8000) + "..." : text;

            var prompt = $@"Analyze the sentiment of this financial document. 
Return JSON with:
- score: number from -1.0 (very negative) to 1.0 (very positive)
- label: one of 'Positive', 'Neutral', 'Negative'

Document:
{truncatedText}";

            var response = await CallOpenAiAsync(prompt, 200);

            if (!response.IsSuccess)
                return Result<(double, string)>.Failure(response.Error ?? "Sentiment analysis failed");

            var content = response.Data!.GetProperty("content").GetString() ?? "{}";
            var sentimentData = JsonSerializer.Deserialize<JsonElement>(content);

            var score = sentimentData.GetProperty("score").GetDouble();
            var label = sentimentData.GetProperty("label").GetString() ?? "Neutral";

            return Result<(double, string)>.Success((score, label));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing sentiment");
            return Result<(double, string)>.Failure($"Sentiment analysis failed: {ex.Message}");
        }
    }

    // Private helper methods

    private string BuildAnalysisPrompt(string text, string companyName, string reportType)
    {
        return $@"You are a senior financial analyst. Analyze this {reportType} for {companyName} and provide comprehensive, detailed insights suitable for investment decision-making.

IMPORTANT: Return your analysis as valid JSON with these exact keys (all required):
{{
  ""executive_summary"": ""Provide a detailed 4-6 sentence summary that includes: (1) Overall company performance and key financial results, (2) Major revenue drivers and segment performance, (3) Geographic or market highlights, (4) Year-over-year growth rates where available, (5) Strategic initiatives and management outlook. Be specific with numbers and metrics."",
  ""key_highlights"": [""highlight with metrics"", ""highlight with metrics"", ""highlight with metrics"", ...],
  ""financial_metrics"": {{""total_revenue"": ""value"", ""revenue_growth"": ""value"", ""ebitda"": ""value"", ""margin"": ""value"", ""eps"": ""value""}},
  ""strategic_initiatives"": ""List major strategic initiatives including acquisitions, partnerships, product launches, market expansion, and technology investments. Provide specific examples and expected impact."",
  ""market_outlook"": ""Detailed assessment of market conditions, competitive landscape, growth drivers, and headwinds. Include specific market segments and geographies."",
  ""risk_factors"": [""specific risk with detail"", ""specific risk with detail"", ...],
  ""competitive_position"": ""Detailed assessment of competitive advantages, market share trends, differentiation factors, and competitive threats."",
  ""investment_thesis"": ""Comprehensive investment perspective covering valuation, growth prospects, risks vs. rewards, and suitability for different investor types."",
  ""sentiment_score"": -1.0 to 1.0 based on overall tone and outlook,
  ""sentiment_label"": ""Positive/Neutral/Negative"",
  ""tags"": [""industry tag"", ""strategy tag"", ""risk tag"", ...],
  ""related_entities"": [""competitor1"", ""partner1"", ""market1"", ...]
}}

CRITICAL REQUIREMENTS:
- executive_summary MUST be detailed and multi-sentence with specific financial data
- Include actual numbers, percentages, and growth rates where mentioned in the document
- For each segment/region, include performance metrics
- Highlight both strengths and concerns
- Make the summary actionable for investors

Document text:
{text}";
    }

    private async Task<Result<JsonElement>> CallOpenAiAsync(string prompt, int maxTokens)
    {
        try
        {
            var requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "system", content = "You are a financial analyst expert. Always return valid JSON." },
                    new { role = "user", content = prompt }
                },
                temperature = _temperature,
                max_tokens = maxTokens
            };

            var response = await _httpClient.PostAsJsonAsync("chat/completions", requestBody);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("OpenAI API error: {Status} - {Error}", response.StatusCode, error);
                return Result<JsonElement>.Failure($"OpenAI API error: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            var content = result.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

            if (string.IsNullOrWhiteSpace(content))
            {
                return Result<JsonElement>.Failure("Empty response from OpenAI");
            }

            // Try to parse as JSON
            try
            {
                var jsonData = JsonSerializer.Deserialize<JsonElement>(content);
                return Result<JsonElement>.Success(jsonData);
            }
            catch
            {
                // If not valid JSON, wrap it
                var wrapped = JsonSerializer.Deserialize<JsonElement>($"{{\"content\": {JsonSerializer.Serialize(content)}}}");
                return Result<JsonElement>.Success(wrapped);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling OpenAI API");
            return Result<JsonElement>.Failure($"OpenAI API call failed: {ex.Message}");
        }
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

    private int? GetOptionalInt(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number 
            ? prop.GetInt32() 
            : null;
    }

    private JsonElement GetOptionalArray(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) ? prop : JsonSerializer.Deserialize<JsonElement>("[]");
    }
}
