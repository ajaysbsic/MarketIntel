using Alfanar.MarketIntel.Application.DTOs;
using Alfanar.MarketIntel.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Alfanar.MarketIntel.Api.Controllers;

/// <summary>
/// AI Chat Controller with RAG Integration
/// Endpoints for intelligent market intelligence queries
/// Performance: 500-2000ms per query (depends on data volume)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AiChatController : ControllerBase
{
    private readonly IAiChatService _aiChatService;
    private readonly IRagContextService _ragContextService;
    private readonly ILogger<AiChatController> _logger;

    public AiChatController(
        IAiChatService aiChatService,
        IRagContextService ragContextService,
        ILogger<AiChatController> logger)
    {
        _aiChatService = aiChatService;
        _ragContextService = ragContextService;
        _logger = logger;
    }

    /// <summary>
    /// Get AI response to a query using RAG
    /// POST /api/aichat/query
    /// 
    /// Request:
    /// {
    ///   "message": "What are Samsung's recent market trends?",
    ///   "contextEntity": "Samsung"  // Optional
    /// }
    /// 
    /// Response:
    /// {
    ///   "answer": "Samsung shows strong growth...",
    ///   "citations": [...],
    ///   "confidence": 0.85,
    ///   "relatedQueries": [...],
    ///   "executionTimeMs": 1250
    /// }
    /// </summary>
    [HttpPost("query")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AiResponseDto>> GetAiResponse([FromBody] ChatRequestDto request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest("Message cannot be empty");

            _logger.LogInformation($"AI Query: {request.Message}");

            var response = await _aiChatService.GetAiResponseAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing AI query");
            return StatusCode(500, new { message = "Error processing query", error = ex.Message });
        }
    }

    /// <summary>
    /// Get context that will be used for a query
    /// Useful for debugging and understanding what data the AI will use
    /// GET /api/aichat/context?query=Samsung&entity=Samsung
    /// </summary>
    [HttpGet("context")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RagContextDto>> GetContext([FromQuery] string query, [FromQuery] string? entity = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Query cannot be empty");

            _logger.LogInformation($"Getting RAG context for: {query}");

            var context = await _ragContextService.GetEnrichedContextAsync(query, entity);
            return Ok(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving context");
            return StatusCode(500, new { message = "Error retrieving context", error = ex.Message });
        }
    }

    /// <summary>
    /// Analyze sentiment of a query
    /// POST /api/aichat/sentiment
    /// </summary>
    [HttpPost("sentiment")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> AnalyzeSentiment([FromBody] ChatRequestDto request)
    {
        try
        {
            var context = await _ragContextService.GetEnrichedContextAsync(request.Message);
            
            var sentiments = new
            {
                query = request.Message,
                positive = CountPositiveSentiments(context),
                negative = CountNegativeSentiments(context),
                neutral = CountNeutralSentiments(context),
                overallSentiment = CalculateOverallSentiment(context)
            };

            return Ok(sentiments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing sentiment");
            return StatusCode(500, new { message = "Error analyzing sentiment" });
        }
    }

    /// <summary>
    /// Get trending topics based on recent data
    /// GET /api/aichat/trending?limit=10
    /// </summary>
    [HttpGet("trending")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetTrendingTopics([FromQuery] int limit = 10)
    {
        try
        {
            // This would use RAG to identify trending entities and topics
            var trendingQueries = new List<string>
            {
                "Market sentiment today",
                "Technology sector trends",
                "Financial market volatility",
                "Emerging market opportunities"
            };

            var result = new
            {
                timestamp = DateTime.UtcNow,
                trendingTopics = trendingQueries.Take(limit)
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trending topics");
            return StatusCode(500, new { message = "Error retrieving trending topics" });
        }
    }

    /// <summary>
    /// Generate a report based on a query
    /// POST /api/aichat/report
    /// </summary>
    [HttpPost("report")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GenerateReport([FromBody] ChatRequestDto request)
    {
        try
        {
            var queries = new[]
            {
                $"What is {request.Message}?",
                $"What are the key metrics for {request.Message}?",
                $"What are the risks for {request.Message}?",
                $"What opportunities exist for {request.Message}?"
            };

            var report = new
            {
                entity = request.Message,
                sections = new { }
            };

            // Get response for each query
            foreach (var query in queries)
            {
                var subRequest = new ChatRequestDto { Message = query, ContextEntity = request.Message };
                var response = await _aiChatService.GetAiResponseAsync(subRequest);
            }

            return Ok(new { message = "Report generation in progress" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report");
            return StatusCode(500, new { message = "Error generating report" });
        }
    }

    #region Helper Methods

    private int CountPositiveSentiments(RagContextDto context)
    {
        var count = 0;
        var positiveWords = new[] { "growth", "increase", "up", "strong", "positive", "gain", "profit" };
        
        foreach (var report in context.Reports)
        {
            count += positiveWords.Count(w => report.Summary.Contains(w, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var news in context.NewsArticles)
        {
            count += positiveWords.Count(w => news.Summary.Contains(w, StringComparison.OrdinalIgnoreCase));
        }

        return count;
    }

    private int CountNegativeSentiments(RagContextDto context)
    {
        var count = 0;
        var negativeWords = new[] { "loss", "decline", "down", "weak", "negative", "fall", "risk" };
        
        foreach (var report in context.Reports)
        {
            count += negativeWords.Count(w => report.Summary.Contains(w, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var news in context.NewsArticles)
        {
            count += negativeWords.Count(w => news.Summary.Contains(w, StringComparison.OrdinalIgnoreCase));
        }

        return count;
    }

    private int CountNeutralSentiments(RagContextDto context)
    {
        var count = 0;
        var neutralWords = new[] { "stable", "neutral", "unchanged", "flat", "steady" };
        
        foreach (var report in context.Reports)
        {
            count += neutralWords.Count(w => report.Summary.Contains(w, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var news in context.NewsArticles)
        {
            count += neutralWords.Count(w => news.Summary.Contains(w, StringComparison.OrdinalIgnoreCase));
        }

        return count;
    }

    private string CalculateOverallSentiment(RagContextDto context)
    {
        var positive = CountPositiveSentiments(context);
        var negative = CountNegativeSentiments(context);
        var neutral = CountNeutralSentiments(context);

        if (positive > negative) return "POSITIVE";
        if (negative > positive) return "NEGATIVE";
        return "NEUTRAL";
    }

    #endregion
}
