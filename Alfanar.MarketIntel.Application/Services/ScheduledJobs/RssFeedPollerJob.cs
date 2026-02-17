using System.Net;
using System.ServiceModel.Syndication;
using System.Xml;
using Alfanar.MarketIntel.Application.DTOs;
using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Application.Interfaces.ScheduledJobs;
using Alfanar.MarketIntel.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Alfanar.MarketIntel.Application.Services.ScheduledJobs;

public class RssFeedPollerJob : IRssFeedPollerJob
{
    private readonly IRssFeedRepository _feedRepository;
    private readonly INewsService _newsService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IJobOrchestrationService _jobOrchestrationService;
    private readonly ILogger<RssFeedPollerJob> _logger;

    public RssFeedPollerJob(
        IRssFeedRepository feedRepository,
        INewsService newsService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IJobOrchestrationService jobOrchestrationService,
        ILogger<RssFeedPollerJob> logger)
    {
        _feedRepository = feedRepository;
        _newsService = newsService;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _jobOrchestrationService = jobOrchestrationService;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var feeds = await _feedRepository.GetActiveAsync();
        if (feeds.Count == 0)
        {
            _logger.LogInformation("RSS poller found no active feeds.");
            return;
        }

        var newItemsIngested = 0;
        foreach (var feed in feeds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            newItemsIngested += await ProcessFeedAsync(feed, cancellationToken);
        }

        if (newItemsIngested > 0)
        {
            _jobOrchestrationService.EnqueueJob<IAlertProcessingJob>(job => job.ExecuteAsync(default));
        }
    }

    private async Task<int> ProcessFeedAsync(Domain.Entities.RssFeed feed, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(feed.Url))
        {
            _logger.LogWarning("RSS feed {FeedId} has no URL configured.", feed.Id);
            return 0;
        }

        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, feed.Url);

        var userAgent = _configuration["RssFeedManagement:UserAgent"];
        if (!string.IsNullOrWhiteSpace(userAgent))
        {
            request.Headers.TryAddWithoutValidation("User-Agent", userAgent);
        }

        if (!string.IsNullOrWhiteSpace(feed.LastETag))
        {
            request.Headers.TryAddWithoutValidation("If-None-Match", feed.LastETag);
        }

        if (!string.IsNullOrWhiteSpace(feed.LastModified) && DateTimeOffset.TryParse(feed.LastModified, out var lastModified))
        {
            request.Headers.IfModifiedSince = lastModified;
        }

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RSS poller failed to fetch feed {FeedName}", feed.Name);
            return 0;
        }

        if (response.StatusCode == HttpStatusCode.NotModified)
        {
            feed.LastFetchedUtc = DateTime.UtcNow;
            await _feedRepository.UpdateAsync(feed);
            await _feedRepository.SaveChangesAsync();
            _logger.LogInformation("RSS feed {FeedName} not modified.", feed.Name);
            return 0;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("RSS poller received {StatusCode} for feed {FeedName}", response.StatusCode, feed.Name);
            return 0;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { Async = true });
        var syndicationFeed = SyndicationFeed.Load(reader);

        if (syndicationFeed == null)
        {
            _logger.LogWarning("RSS poller could not parse feed {FeedName}", feed.Name);
            return 0;
        }

        var newCount = 0;
        foreach (var item in syndicationFeed.Items ?? Enumerable.Empty<SyndicationItem>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var link = item.Links.FirstOrDefault()?.Uri?.ToString();
            var title = item.Title?.Text ?? string.Empty;

            if (string.IsNullOrWhiteSpace(link) || string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            var summary = item.Summary?.Text ?? string.Empty;
            var content = item.Content as TextSyndicationContent;
            var bodyText = content?.Text ?? summary;

            var publishDate = item.PublishDate != DateTimeOffset.MinValue
                ? item.PublishDate.UtcDateTime
                : (item.LastUpdatedTime != DateTimeOffset.MinValue ? item.LastUpdatedTime.UtcDateTime : DateTime.UtcNow);

            var tags = item.Categories
                .Select(c => c.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!string.IsNullOrWhiteSpace(feed.Category) && !tags.Contains(feed.Category))
            {
                tags.Add(feed.Category);
            }

            var ingestRequest = new IngestNewsRequest
            {
                Source = feed.Name,
                Url = link,
                Title = title,
                PublishedUtc = publishDate,
                Region = feed.Region,
                Summary = summary,
                BodyText = bodyText,
                Tags = tags
            };

            var result = await _newsService.IngestArticleAsync(ingestRequest);
            if (result.IsSuccess)
            {
                newCount++;
            }
        }

        feed.LastFetchedUtc = DateTime.UtcNow;
        if (response.Headers.ETag != null)
        {
            feed.LastETag = response.Headers.ETag.Tag ?? response.Headers.ETag.ToString();
        }

        if (response.Content.Headers.LastModified.HasValue)
        {
            feed.LastModified = response.Content.Headers.LastModified.Value.ToString("R");
        }

        await _feedRepository.UpdateAsync(feed);
        await _feedRepository.SaveChangesAsync();

        _logger.LogInformation("RSS poller ingested {Count} new items from {FeedName}", newCount, feed.Name);
        return newCount;
    }
}
