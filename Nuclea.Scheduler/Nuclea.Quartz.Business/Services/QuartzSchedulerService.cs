using Microsoft.Extensions.Logging;
using Nuclea.Quartz.Business.Attributes;
using Nuclea.Quartz.Business.Interfaces;
using Quartz;
using Quartz.Impl.Matchers;

namespace Nuclea.Quartz.Business.Services;

/// <summary>
/// Service for managing Quartz scheduler operations
/// </summary>
public class QuartzSchedulerService(
    ISchedulerFactory schedulerFactory,
    ILogger<QuartzSchedulerService> logger)
    : IQuartzSchedulerService
{
    private IScheduler? _scheduler;

    private async Task<IScheduler> GetSchedulerAsync()
    {
        _scheduler ??= await schedulerFactory.GetScheduler();
        return _scheduler;
    }

    public bool IsStarted => _scheduler?.IsStarted ?? false;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var scheduler = await GetSchedulerAsync();
        if (!scheduler.IsStarted)
        {
            await scheduler.Start(cancellationToken);
            logger.LogInformation("Quartz scheduler started");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        var scheduler = await GetSchedulerAsync();
        if (scheduler.IsStarted)
        {
            await scheduler.Shutdown(true, cancellationToken);
            logger.LogInformation("Quartz scheduler stopped");
        }
    }

    public async Task ScheduleJobAsync<TJob>(string cronExpression, Dictionary<string, object>? jobData = null,
        bool startNow = false)
        where TJob : IQuartzJob
    {
        var scheduler = await GetSchedulerAsync();
        if (!scheduler.IsStarted)
        {
            throw new InvalidOperationException("Scheduler is not started. Call StartAsync first.");
        }

        var jobMetadata = GetJobMetadata<TJob>();
        var jobKey = new JobKey(jobMetadata.JobName, jobMetadata.JobGroup);

        // Check if job already exists
        if (await scheduler.CheckExists(jobKey))
        {
            logger.LogWarning("Job {JobName} in group {JobGroup} already exists. Skipping registration.",
                jobMetadata.JobName, jobMetadata.JobGroup);
            return;
        }

        var jobBuilder = JobBuilder.Create<TJob>()
            .WithIdentity(jobKey)
            .WithDescription(jobMetadata.Description);

        if (jobData != null && jobData.Count > 0)
        {
            var jobDataMap = new JobDataMap();
            foreach (var data in jobData)
            {
                jobDataMap.Put(data.Key, data.Value);
            }

            jobBuilder.SetJobData(jobDataMap);
        }

        var job = jobBuilder.Build();

        var triggerBuilder = TriggerBuilder.Create()
            .WithIdentity($"{jobMetadata.JobName}_trigger", jobMetadata.JobGroup)
            .WithCronSchedule(cronExpression);

        if (startNow)
        {
            triggerBuilder.StartNow();
        }

        var trigger = triggerBuilder.Build();

        await scheduler.ScheduleJob(job, trigger);
        logger.LogInformation("Job {JobName} in group {JobGroup} scheduled with cron expression: {CronExpression}",
            jobMetadata.JobName, jobMetadata.JobGroup, cronExpression);
    }

    public async Task ScheduleJobAsync<TJob>(ITrigger trigger, Dictionary<string, object>? jobData = null)
        where TJob : IQuartzJob
    {
        var scheduler = await GetSchedulerAsync();
        if (!scheduler.IsStarted)
        {
            throw new InvalidOperationException("Scheduler is not started. Call StartAsync first.");
        }

        var jobMetadata = GetJobMetadata<TJob>();
        var jobKey = new JobKey(jobMetadata.JobName, jobMetadata.JobGroup);

        // Check if job already exists
        if (await scheduler.CheckExists(jobKey))
        {
            logger.LogWarning("Job {JobName} in group {JobGroup} already exists. Skipping registration.",
                jobMetadata.JobName, jobMetadata.JobGroup);
            return;
        }

        var jobBuilder = JobBuilder.Create<TJob>()
            .WithIdentity(jobKey)
            .WithDescription(jobMetadata.Description);

        if (jobData != null && jobData.Count > 0)
        {
            var jobDataMap = new JobDataMap();
            foreach (var data in jobData)
            {
                jobDataMap.Put(data.Key, data.Value);
            }

            jobBuilder.SetJobData(jobDataMap);
        }

        var job = jobBuilder.Build();

        await scheduler.ScheduleJob(job, trigger);
        logger.LogInformation("Job {JobName} in group {JobGroup} scheduled with custom trigger",
            jobMetadata.JobName, jobMetadata.JobGroup);
    }

    public async Task PauseJobAsync(string jobName, string jobGroup)
    {
        var scheduler = await GetSchedulerAsync();
        if (!scheduler.IsStarted)
        {
            throw new InvalidOperationException("Scheduler is not started. Call StartAsync first.");
        }

        var jobKey = new JobKey(jobName, jobGroup);
        await scheduler.PauseJob(jobKey);
        logger.LogInformation("Job {JobName} in group {JobGroup} paused", jobName, jobGroup);
    }

    public async Task ResumeJobAsync(string jobName, string jobGroup)
    {
        var scheduler = await GetSchedulerAsync();
        if (!scheduler.IsStarted)
        {
            throw new InvalidOperationException("Scheduler is not started. Call StartAsync first.");
        }

        var jobKey = new JobKey(jobName, jobGroup);
        await scheduler.ResumeJob(jobKey);
        logger.LogInformation("Job {JobName} in group {JobGroup} resumed", jobName, jobGroup);
    }

    public async Task DeleteJobAsync(string jobName, string jobGroup)
    {
        var scheduler = await GetSchedulerAsync();
        if (!scheduler.IsStarted)
        {
            throw new InvalidOperationException("Scheduler is not started. Call StartAsync first.");
        }

        var jobKey = new JobKey(jobName, jobGroup);
        var deleted = await scheduler.DeleteJob(jobKey);

        if (deleted)
        {
            logger.LogInformation("Job {JobName} in group {JobGroup} deleted", jobName, jobGroup);
        }
        else
        {
            logger.LogWarning("Job {JobName} in group {JobGroup} not found for deletion", jobName, jobGroup);
        }
    }

    public async Task TriggerJobAsync(string jobName, string jobGroup, Dictionary<string, object>? jobData = null)
    {
        var scheduler = await GetSchedulerAsync();
        if (!scheduler.IsStarted)
        {
            throw new InvalidOperationException("Scheduler is not started. Call StartAsync first.");
        }

        var jobKey = new JobKey(jobName, jobGroup);
        JobDataMap? jobDataMap = null;

        if (jobData != null && jobData.Count > 0)
        {
            jobDataMap = new JobDataMap();
            foreach (var data in jobData)
            {
                jobDataMap.Put(data.Key, data.Value);
            }
        }

        await scheduler.TriggerJob(jobKey, jobDataMap ?? new JobDataMap());
        logger.LogInformation("Job {JobName} in group {JobGroup} triggered manually", jobName, jobGroup);
    }

    public async Task<IReadOnlyCollection<IJobExecutionContext>> GetCurrentlyExecutingJobsAsync()
    {
        var scheduler = await GetSchedulerAsync();
        if (!scheduler.IsStarted)
        {
            throw new InvalidOperationException("Scheduler is not started. Call StartAsync first.");
        }

        return await scheduler.GetCurrentlyExecutingJobs();
    }

    public async Task<IJobDetail?> GetJobDetailAsync(string jobName, string jobGroup)
    {
        var scheduler = await GetSchedulerAsync();
        if (!scheduler.IsStarted)
        {
            throw new InvalidOperationException("Scheduler is not started. Call StartAsync first.");
        }

        var jobKey = new JobKey(jobName, jobGroup);
        return await scheduler.GetJobDetail(jobKey);
    }

    public async Task<IReadOnlyCollection<JobKey>> GetJobKeysAsync()
    {
        var scheduler = await GetSchedulerAsync();
        if (!scheduler.IsStarted)
        {
            throw new InvalidOperationException("Scheduler is not started. Call StartAsync first.");
        }

        return await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
    }

    private static (string JobName, string JobGroup, string Description) GetJobMetadata<TJob>() where TJob : IQuartzJob
    {
        var jobType = typeof(TJob);
        var attribute = jobType.GetCustomAttributes(typeof(QuartzJobAttribute), false)
            .FirstOrDefault() as QuartzJobAttribute;

        if (attribute != null)
        {
            return (attribute.JobName, attribute.JobGroup, attribute.Description);
        }

        // Fallback: use type name
        return (jobType.Name, "Default", $"Job {jobType.Name}");
    }
}
