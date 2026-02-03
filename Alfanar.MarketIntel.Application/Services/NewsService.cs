using Alfanar.MarketIntel.Application.Common;
using Alfanar.MarketIntel.Application.DTOs;
using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Domain.Entities;
using Alfanar.MarketIntel.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace Alfanar.MarketIntel.Application.Services;

public class NewsService : INewsService
{
    private readonly INewsRepository _newsRepository;
    private readonly ITagRepository _tagRepository;
    private readonly ICategoryClassifier _classifier;
    private readonly ILogger<NewsService> _logger;

    public NewsService(
        INewsRepository newsRepository,
        ITagRepository tagRepository,
        ICategoryClassifier classifier,
        ILogger<NewsService> logger)
    {
        _newsRepository = newsRepository;
        _tagRepository = tagRepository;
        _classifier = classifier;
        _logger = logger;
    }

    public async Task<Result<NewsArticleDto>> IngestArticleAsync(IngestNewsRequest request)
    {
        try
        {
            // Check for duplicates
            if (await _newsRepository.ExistsByUrlAsync(request.Url))
            {
                _logger.LogWarning("Duplicate article URL attempted: {Url}", request.Url);
                return Result<NewsArticleDto>.Failure("Article with this URL already exists");
            }

            // Create article entity
            var article = new NewsArticle
            {
                Id = Guid.NewGuid(),
                Source = request.Source,
                Url = request.Url,
                Title = request.Title,
                PublishedUtc = request.PublishedUtc ?? DateTime.UtcNow,
                Region = request.Region ?? "Global",
                Summary = request.Summary ?? "",
                BodyText = request.BodyText ?? "",
                CreatedUtc = DateTime.UtcNow,
                IsProcessed = false
            };

            // Add article first
            await _newsRepository.AddAsync(article);
            await _newsRepository.SaveChangesAsync();

            // Process tags if provided
            if (request.Tags != null && request.Tags.Any())
            {
                foreach (var tagName in request.Tags.Where(t => !string.IsNullOrWhiteSpace(t)))
                {
                    var tag = await _tagRepository.GetOrCreateAsync(tagName.Trim());
                    article.NewsArticleTags.Add(new NewsArticleTag
                    {
                        NewsArticleId = article.Id,
                        TagId = tag.Id
                    });
                }
                await _newsRepository.SaveChangesAsync();
            }

            // Classify and process article
            var (category, summary, confidence) = await _classifier.ClassifyAndSummarizeAsync(
                article.Title, 
                article.BodyText);

            article.Category = category;
            article.ClassificationConfidence = confidence;
            
            // Use generated summary if original was empty
            if (string.IsNullOrWhiteSpace(article.Summary))
                article.Summary = summary;
            
            article.IsProcessed = true;
            article.ProcessedUtc = DateTime.UtcNow;

            await _newsRepository.UpdateAsync(article);
            await _newsRepository.SaveChangesAsync();

            _logger.LogInformation("Successfully ingested article: {Title} [{Category}]", 
                article.Title, article.Category);

            return Result<NewsArticleDto>.Success(MapToDto(article));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ingesting article: {Title}", request.Title);
            return Result<NewsArticleDto>.Failure($"Failed to ingest article: {ex.Message}");
        }
    }

    public async Task<Result<NewsArticleDto>> GetByIdAsync(Guid id)
    {
        try
        {
            var article = await _newsRepository.GetByIdAsync(id);
            if (article == null)
                return Result<NewsArticleDto>.Failure("Article not found");

            return Result<NewsArticleDto>.Success(MapToDto(article));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving article {Id}", id);
            return Result<NewsArticleDto>.Failure($"Failed to retrieve article: {ex.Message}");
        }
    }

    public async Task<Result<PaginatedList<NewsArticleDto>>> GetFilteredArticlesAsync(NewsFilterDto filter)
    {
        try
        {
            var articles = await _newsRepository.GetFilteredAsync(
                filter.Category,
                filter.Region,
                filter.FromDate,
                filter.ToDate,
                filter.SearchTerm,
                filter.Tags,
                filter.PageNumber,
                filter.PageSize);

            var totalCount = await _newsRepository.GetFilteredCountAsync(
                filter.Category,
                filter.Region,
                filter.FromDate,
                filter.ToDate,
                filter.SearchTerm,
                filter.Tags);

            var dtos = articles.Select(MapToDto).ToList();
            var paginatedList = new PaginatedList<NewsArticleDto>(
                dtos, 
                totalCount, 
                filter.PageNumber, 
                filter.PageSize);

            return Result<PaginatedList<NewsArticleDto>>.Success(paginatedList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving filtered articles");
            return Result<PaginatedList<NewsArticleDto>>.Failure($"Failed to retrieve articles: {ex.Message}");
        }
    }

    public async Task<Result<List<NewsArticleDto>>> GetRecentArticlesAsync(int count = 10)
    {
        try
        {
            var articles = await _newsRepository.GetRecentAsync(count);
            var dtos = articles.Select(MapToDto).ToList();
            return Result<List<NewsArticleDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent articles");
            return Result<List<NewsArticleDto>>.Failure($"Failed to retrieve recent articles: {ex.Message}");
        }
    }

    public async Task<Result> DeleteArticleAsync(Guid id)
    {
        try
        {
            var article = await _newsRepository.GetByIdAsync(id);
            if (article == null)
                return Result.Failure("Article not found");

            await _newsRepository.DeleteAsync(article);
            await _newsRepository.SaveChangesAsync();

            _logger.LogInformation("Deleted article {Id}", id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting article {Id}", id);
            return Result.Failure($"Failed to delete article: {ex.Message}");
        }
    }

    public async Task<Result<List<string>>> GetAllCategoriesAsync()
    {
        try
        {
            var categories = await _newsRepository.GetDistinctCategoriesAsync();
            return Result<List<string>>.Success(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving categories");
            return Result<List<string>>.Failure($"Failed to retrieve categories: {ex.Message}");
        }
    }

    public async Task<Result<List<string>>> GetAllRegionsAsync()
    {
        try
        {
            var regions = await _newsRepository.GetDistinctRegionsAsync();
            return Result<List<string>>.Success(regions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving regions");
            return Result<List<string>>.Failure($"Failed to retrieve regions: {ex.Message}");
        }
    }

    private NewsArticleDto MapToDto(NewsArticle article)
    {
        return new NewsArticleDto
        {
            Id = article.Id,
            Source = article.Source,
            Url = article.Url,
            Title = article.Title,
            PublishedUtc = article.PublishedUtc,
            Region = article.Region,
            Category = article.Category,
            Summary = article.Summary,
            BodyText = article.BodyText,
            Tags = article.NewsArticleTags?.Select(nat => nat.Tag.Name).ToList() ?? new List<string>(),
            IsProcessed = article.IsProcessed,
            CreatedUtc = article.CreatedUtc,
            ProcessedUtc = article.ProcessedUtc
        };
    }
}
