using Nuclea.Common.Business.Interfaces;
using Quartz;

namespace Nuclea.Quartz.Business.Interfaces;

public interface IQuartzSchedulerService : ISingletonService
{
    Task StartAsync(CancellationToken cancellationToken = default);
    
    Task StopAsync(CancellationToken cancellationToken = default);

    Task ScheduleJobAsync<TJob>(string cronExpression, Dictionary<string, object>? jobData = null, bool startNow = false) 
        where TJob : IQuartzJob;
    
    Task ScheduleJobAsync<TJob>(ITrigger trigger, Dictionary<string, object>? jobData = null) 
        where TJob : IQuartzJob;

    Task PauseJobAsync(string jobName, string jobGroup);
    
    Task ResumeJobAsync(string jobName, string jobGroup);
    
    Task DeleteJobAsync(string jobName, string jobGroup);
    
    Task TriggerJobAsync(string jobName, string jobGroup, Dictionary<string, object>? jobData = null);
    
    Task<IReadOnlyCollection<IJobExecutionContext>> GetCurrentlyExecutingJobsAsync();
    
    Task<IJobDetail?> GetJobDetailAsync(string jobName, string jobGroup);
    
    Task<IReadOnlyCollection<JobKey>> GetJobKeysAsync();
    
    bool IsStarted { get; }
}
