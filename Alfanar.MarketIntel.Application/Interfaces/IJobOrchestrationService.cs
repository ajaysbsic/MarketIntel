using System.Linq.Expressions;

namespace Alfanar.MarketIntel.Application.Interfaces;

public interface IJobOrchestrationService
{
    string ScheduleRecurringJob<TJob>(string jobName, string cronExpression, Expression<Func<TJob, Task>> action);
    string EnqueueJob<TJob>(Expression<Func<TJob, Task>> action, TimeSpan? delay = null);
    string EnqueueContinuation<TJob>(string parentJobId, Expression<Func<TJob, Task>> action);
}
