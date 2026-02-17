using Alfanar.MarketIntel.Application.DTOs;
using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Application.Interfaces.ScheduledJobs;
using Alfanar.MarketIntel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Alfanar.MarketIntel.Application.Services.ScheduledJobs;

public class KeywordMonitorJob : IKeywordMonitorJob
{
    private readonly IKeywordMonitorService _monitorService;
    private readonly IWebSearchService _webSearchService;
    private readonly IEnumerable<IWebSearchProvider> _providers;
    private readonly MarketIntelDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IJobOrchestrationService _jobOrchestrationService;
    private readonly ILogger<KeywordMonitorJob> _logger;

    public KeywordMonitorJob(
        IKeywordMonitorService monitorService,
        IWebSearchService webSearchService,
        IEnumerable<IWebSearchProvider> providers,
        MarketIntelDbContext context,
        IConfiguration configuration,
        IJobOrchestrationService jobOrchestrationService,
        ILogger<KeywordMonitorJob> logger)
    {
        _monitorService = monitorService;
        _webSearchService = webSearchService;
        _providers = providers;
        _context = context;
        _configuration = configuration;
        _jobOrchestrationService = jobOrchestrationService;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var intervalMinutes = _configuration.GetValue("KeywordMonitoring:DefaultCheckIntervalMinutes", 60);
        var dueResult = await _monitorService.GetMonitorsDueForCheckAsync(intervalMinutes);

        if (!dueResult.IsSuccess || dueResult.Data == null)
        {
            _logger.LogWarning("Keyword monitor job could not load due monitors: {Error}", dueResult.Error);
            return;
        }

        if (dueResult.Data.Count == 0)
        {
            _logger.LogInformation("Keyword monitor job found no monitors due for check.");
            return;
        }

        var ingestedResults = false;
        foreach (var monitor in dueResult.Data)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var didIngest = await ProcessMonitorAsync(monitor, cancellationToken);
            ingestedResults = ingestedResults || didIngest;
        }

        if (ingestedResults)
        {
            _jobOrchestrationService.EnqueueJob<IAlertProcessingJob>(job => job.ExecuteAsync(default));
        }
    }

    private async Task<bool> ProcessMonitorAsync(KeywordMonitorDto monitor, CancellationToken cancellationToken)
    {
        var provider = SelectProvider();
        if (provider == null)
        {
            _logger.LogWarning("Keyword monitor job found no configured search provider.");
            return false;
        }

        var searchRequest = new WebSearchRequestDto
        {
            Keyword = monitor.Keyword,
            MaxResults = monitor.MaxResultsPerCheck,
            SearchProvider = provider.ProviderName
        };

        List<WebSearchResultDto> results;
        try
        {
            results = await provider.SearchAsync(searchRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Keyword monitor job failed search for {Keyword}", monitor.Keyword);
            return false;
        }

        if (results.Count == 0)
        {
            await UpdateLastCheckedAsync(monitor.Id, cancellationToken);
            return false;
        }

        var ingestionRequest = new WebSearchRequestDto
        {
            Keyword = monitor.Keyword,
            MaxResults = monitor.MaxResultsPerCheck,
            SearchProvider = provider.ProviderName,
            Results = results
        };

        var ingestionResult = await _webSearchService.SearchAsync(ingestionRequest);
        if (!ingestionResult.IsSuccess)
        {
            _logger.LogWarning("Keyword monitor job failed to ingest results for {Keyword}: {Error}", monitor.Keyword, ingestionResult.Error);
        }

        await UpdateLastCheckedAsync(monitor.Id, cancellationToken);
        return ingestionResult.IsSuccess;
    }

    private IWebSearchProvider? SelectProvider()
    {
        var preferred = _configuration["KeywordMonitoring:SearchProvider"];
        if (!string.IsNullOrWhiteSpace(preferred))
        {
            var provider = _providers.FirstOrDefault(p => p.ProviderName.Equals(preferred, StringComparison.OrdinalIgnoreCase));
            if (provider != null && provider.IsConfigured())
            {
                return provider;
            }
        }

        var newsApi = _providers.FirstOrDefault(p => p.ProviderName.Equals("newsapi", StringComparison.OrdinalIgnoreCase));
        if (newsApi != null && newsApi.IsConfigured())
        {
            return newsApi;
        }

        var google = _providers.FirstOrDefault(p => p.ProviderName.Equals("google", StringComparison.OrdinalIgnoreCase));
        if (google != null && google.IsConfigured())
        {
            return google;
        }

        return null;
    }

    private async Task UpdateLastCheckedAsync(Guid monitorId, CancellationToken cancellationToken)
    {
        var entity = await _context.KeywordMonitors.FirstOrDefaultAsync(m => m.Id == monitorId, cancellationToken);
        if (entity == null)
        {
            return;
        }

        entity.LastCheckedUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
