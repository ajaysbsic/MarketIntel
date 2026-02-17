using System.Linq.Expressions;
using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Application.Interfaces.ScheduledJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Alfanar.MarketIntel.Api.Services;

public class JobSchedulingService : IHostedService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<JobSchedulingService> _logger;

    public JobSchedulingService(
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        ILogger<JobSchedulingService> logger)
    {
        _configuration = configuration;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var orchestrationService = scope.ServiceProvider.GetRequiredService<IJobOrchestrationService>();
        RegisterRecurringJobs(orchestrationService);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void RegisterRecurringJobs(IJobOrchestrationService orchestrationService)
    {
        var jobsSection = _configuration.GetSection("Jobs");

        RegisterJob<IRssFeedPollerJob>(
            "rss-poller",
            jobsSection.GetSection("RssPoller"),
            "*/5 * * * *",
            job => job.ExecuteAsync(default),
            orchestrationService);

        RegisterJob<IKeywordMonitorJob>(
            "keyword-monitor",
            jobsSection.GetSection("KeywordMonitor"),
            "*/5 * * * *",
            job => job.ExecuteAsync(default),
            orchestrationService);

        RegisterJob<ITrendSnapshotJob>(
            "trend-snapshot",
            jobsSection.GetSection("TrendSnapshot"),
            "0 0 * * *",
            job => job.ExecuteAsync(default),
            orchestrationService);

        RegisterJob<IAlertProcessingJob>(
            "alert-processing",
            jobsSection.GetSection("AlertProcessing"),
            "*/1 * * * *",
            job => job.ExecuteAsync(default),
            orchestrationService);

        RegisterJob<INotificationQueueJob>(
            "notification-queue-processor",
            jobsSection.GetSection("NotificationQueue"),
            "*/1 * * * *",
            job => job.ExecuteAsync(default),
            orchestrationService);
    }

    private void RegisterJob<TJob>(
        string jobName,
        IConfigurationSection section,
        string defaultCron,
        Expression<Func<TJob, Task>> action,
        IJobOrchestrationService orchestrationService)
    {
        var enabled = section.GetValue("Enabled", true);
        var cron = section["CronExpression"] ?? defaultCron;

        if (!enabled)
        {
            _logger.LogInformation("Hangfire job {JobName} is disabled.", jobName);
            return;
        }

        if (string.IsNullOrWhiteSpace(cron))
        {
            _logger.LogWarning("Hangfire job {JobName} has no cron expression configured.", jobName);
            return;
        }

        orchestrationService.ScheduleRecurringJob(jobName, cron, action);
        _logger.LogInformation("Hangfire job {JobName} scheduled with cron {Cron}.", jobName, cron);
    }
}
