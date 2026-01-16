using Microsoft.Extensions.DependencyInjection;
using Nuclea.Quartz.Business.Attributes;
using Nuclea.Quartz.Business.Interfaces;
using Quartz;

namespace Nuclea.Quartz.Business.Extensions;

public static class QuartzJobRegistrationExtensions
{
    
    public static async Task RegisterQuartzJobAsync<TJob>(
        this IServiceProvider serviceProvider,
        string cronExpression,
        Dictionary<string, object>? jobData = null,
        bool startNow = false)
        where TJob : class, IQuartzJob
    {
        var schedulerService = serviceProvider.GetRequiredService<IQuartzSchedulerService>();

        if (!schedulerService.IsStarted)
        {
            await schedulerService.StartAsync();
        }

        await schedulerService.ScheduleJobAsync<TJob>(cronExpression, jobData, startNow);
    }
    
    public static IServiceCollectionQuartzConfigurator AddQuartzJob<TJob>(
        this IServiceCollectionQuartzConfigurator configurator,
        string? cronExpression = null)
        where TJob : class, IJob
    {
        var (jobName, jobGroup, description) = GetJobMetadata<TJob>();
        var jobKey = new JobKey(jobName, jobGroup);

        configurator.AddJob<TJob>(opts => opts
            .WithIdentity(jobKey)
            .WithDescription(description)
            .StoreDurably());

        if (!string.IsNullOrEmpty(cronExpression))
        {
            configurator.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity($"{jobName}_trigger", jobGroup)
                .WithCronSchedule(cronExpression));
        }

        return configurator;
    }
    
    
    public static IServiceCollectionQuartzConfigurator AddQuartzJobWithInterval<TJob>(
        this IServiceCollectionQuartzConfigurator configurator,
        TimeSpan interval,
        bool repeatForever = true)
        where TJob : class, IJob
    {
        var (jobName, jobGroup, description) = GetJobMetadata<TJob>();
        var jobKey = new JobKey(jobName, jobGroup);

        configurator.AddJob<TJob>(opts => opts
            .WithIdentity(jobKey)
            .WithDescription(description)
            .StoreDurably());

        configurator.AddTrigger(opts =>
        {
            opts.ForJob(jobKey)
                .WithIdentity($"{jobName}_trigger", jobGroup)
                .StartNow();

            if (repeatForever)
            {
                opts.WithSimpleSchedule(x => x
                    .WithInterval(interval)
                    .RepeatForever());
            }
            else
            {
                opts.WithSimpleSchedule(x => x
                    .WithInterval(interval));
            }
        });

        return configurator;
    }

    private static (string JobName, string JobGroup, string Description) GetJobMetadata<TJob>() where TJob : class
    {
        var jobType = typeof(TJob);
        var attribute = jobType.GetCustomAttributes(typeof(QuartzJobAttribute), false)
            .FirstOrDefault() as QuartzJobAttribute;

        var jobName = attribute?.JobName ?? jobType.Name;
        var jobGroup = attribute?.JobGroup ?? "Default";
        var description = attribute?.Description ?? $"Job {jobType.Name}";

        return (jobName, jobGroup, description);
    }
}