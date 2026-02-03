using Alfanar.MarketIntel.Application.Common;
using Alfanar.MarketIntel.Application.DTOs;
using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Domain.Entities;
using Alfanar.MarketIntel.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace Alfanar.MarketIntel.Application.Services;

public class RssFeedService : IRssFeedService
{
    private readonly IRssFeedRepository _feedRepository;
    private readonly ILogger<RssFeedService> _logger;

    public RssFeedService(
        IRssFeedRepository feedRepository,
        ILogger<RssFeedService> logger)
    {
        _feedRepository = feedRepository;
        _logger = logger;
    }

    public async Task<Result<RssFeedDto>> CreateFeedAsync(RssFeedDto feedDto)
    {
        try
        {
            // Check for duplicate URL
            if (await _feedRepository.ExistsByUrlAsync(feedDto.Url))
            {
                _logger.LogWarning("Duplicate RSS feed URL attempted: {Url}", feedDto.Url);
                return Result<RssFeedDto>.Failure("Feed with this URL already exists");
            }

            var feed = new RssFeed
            {
                Id = Guid.NewGuid(),
                Name = feedDto.Name,
                Url = feedDto.Url,
                Category = feedDto.Category ?? "General",
                Region = feedDto.Region ?? "Global",
                IsActive = feedDto.IsActive,
                CreatedUtc = DateTime.UtcNow
            };

            await _feedRepository.AddAsync(feed);
            await _feedRepository.SaveChangesAsync();

            _logger.LogInformation("Created RSS feed: {Name}", feed.Name);
            return Result<RssFeedDto>.Success(MapToDto(feed));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating RSS feed: {Name}", feedDto.Name);
            return Result<RssFeedDto>.Failure($"Failed to create feed: {ex.Message}");
        }
    }

    public async Task<Result<RssFeedDto>> GetFeedByIdAsync(Guid id)
    {
        try
        {
            var feed = await _feedRepository.GetByIdAsync(id);
            if (feed == null)
                return Result<RssFeedDto>.Failure("Feed not found");

            return Result<RssFeedDto>.Success(MapToDto(feed));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving feed {Id}", id);
            return Result<RssFeedDto>.Failure($"Failed to retrieve feed: {ex.Message}");
        }
    }

    public async Task<Result<List<RssFeedDto>>> GetAllFeedsAsync()
    {
        try
        {
            var feeds = await _feedRepository.GetAllAsync();
            var dtos = feeds.Select(MapToDto).ToList();
            return Result<List<RssFeedDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all feeds");
            return Result<List<RssFeedDto>>.Failure($"Failed to retrieve feeds: {ex.Message}");
        }
    }

    public async Task<Result<List<RssFeedDto>>> GetActiveFeedsAsync()
    {
        try
        {
            var feeds = await _feedRepository.GetActiveAsync();
            var dtos = feeds.Select(MapToDto).ToList();
            return Result<List<RssFeedDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active feeds");
            return Result<List<RssFeedDto>>.Failure($"Failed to retrieve active feeds: {ex.Message}");
        }
    }

    public async Task<Result> UpdateFeedAsync(Guid id, RssFeedDto feedDto)
    {
        try
        {
            var feed = await _feedRepository.GetByIdAsync(id);
            if (feed == null)
                return Result.Failure("Feed not found");

            // Check if URL is being changed to an existing URL
            if (feed.Url != feedDto.Url && await _feedRepository.ExistsByUrlAsync(feedDto.Url))
                return Result.Failure("Another feed with this URL already exists");

            feed.Name = feedDto.Name;
            feed.Url = feedDto.Url;
            feed.Category = feedDto.Category ?? "General";
            feed.Region = feedDto.Region ?? "Global";
            feed.IsActive = feedDto.IsActive;

            await _feedRepository.UpdateAsync(feed);
            await _feedRepository.SaveChangesAsync();

            _logger.LogInformation("Updated RSS feed: {Name}", feed.Name);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating feed {Id}", id);
            return Result.Failure($"Failed to update feed: {ex.Message}");
        }
    }

    public async Task<Result> UpdateFeedLastFetchedAsync(Guid id, string? etag)
    {
        try
        {
            var feed = await _feedRepository.GetByIdAsync(id);
            if (feed == null)
                return Result.Failure("Feed not found");

            feed.LastFetchedUtc = DateTime.UtcNow;
            feed.LastETag = etag;

            await _feedRepository.UpdateAsync(feed);
            await _feedRepository.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating feed last fetched {Id}", id);
            return Result.Failure($"Failed to update feed: {ex.Message}");
        }
    }

    public async Task<Result> ToggleFeedStatusAsync(Guid id)
    {
        try
        {
            var feed = await _feedRepository.GetByIdAsync(id);
            if (feed == null)
                return Result.Failure("Feed not found");

            feed.IsActive = !feed.IsActive;

            await _feedRepository.UpdateAsync(feed);
            await _feedRepository.SaveChangesAsync();

            _logger.LogInformation("Toggled feed status: {Name} - Active: {IsActive}", 
                feed.Name, feed.IsActive);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling feed status {Id}", id);
            return Result.Failure($"Failed to toggle feed status: {ex.Message}");
        }
    }

    public async Task<Result> DeleteFeedAsync(Guid id)
    {
        try
        {
            var feed = await _feedRepository.GetByIdAsync(id);
            if (feed == null)
                return Result.Failure("Feed not found");

            await _feedRepository.DeleteAsync(feed);
            await _feedRepository.SaveChangesAsync();

            _logger.LogInformation("Deleted RSS feed: {Name}", feed.Name);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting feed {Id}", id);
            return Result.Failure($"Failed to delete feed: {ex.Message}");
        }
    }

    private RssFeedDto MapToDto(RssFeed feed)
    {
        return new RssFeedDto
        {
            Id = feed.Id,
            Name = feed.Name,
            Url = feed.Url,
            Category = feed.Category,
            Region = feed.Region,
            IsActive = feed.IsActive,
            LastFetchedUtc = feed.LastFetchedUtc,
            LastETag = feed.LastETag,
            ArticleCount = feed.Articles?.Count
        };
    }
}
