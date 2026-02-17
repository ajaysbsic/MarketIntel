using System.Linq.Expressions;
using Alfanar.MarketIntel.Application.Interfaces;
using Hangfire;

namespace Alfanar.MarketIntel.Application.Services;

public class JobOrchestrationService : IJobOrchestrationService
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IRecurringJobManager _recurringJobManager;

    public JobOrchestrationService(IBackgroundJobClient backgroundJobClient, IRecurringJobManager recurringJobManager)
    {
        _backgroundJobClient = backgroundJobClient;
        _recurringJobManager = recurringJobManager;
    }

    public string ScheduleRecurringJob<TJob>(string jobName, string cronExpression, Expression<Func<TJob, Task>> action)
    {
        _recurringJobManager.AddOrUpdate(jobName, action, cronExpression);
        return jobName;
    }

    public string EnqueueJob<TJob>(Expression<Func<TJob, Task>> action, TimeSpan? delay = null)
    {
        if (delay.HasValue && delay.Value > TimeSpan.Zero)
        {
            return _backgroundJobClient.Schedule(action, delay.Value);
        }

        return _backgroundJobClient.Enqueue(action);
    }

    public string EnqueueContinuation<TJob>(string parentJobId, Expression<Func<TJob, Task>> action)
    {
        return _backgroundJobClient.ContinueJobWith(parentJobId, action);
    }
}
