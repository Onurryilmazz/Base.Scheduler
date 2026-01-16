using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Nuclea.Data;
using Nuclea.Quartz.Business.Attributes;
using Nuclea.Quartz.Business.Interfaces;
using Quartz;

namespace Nuclea.Quartz.Business.Jobs;

public abstract class BaseQuartzJob(
    ILogger logger,
    IHttpContextAccessor contextAccessor,
    NucleaDataContext context)
    : IQuartzJob
{
    protected readonly ILogger Logger = logger;
    protected IHttpContextAccessor _httpContext = contextAccessor;
    protected NucleaDataContext _context = context;

    public string JobName
    {
        get
        {
            var attribute = GetType().GetCustomAttributes(typeof(QuartzJobAttribute), false)
                .FirstOrDefault() as QuartzJobAttribute;
            return attribute?.JobName ?? GetType().Name;
        }
    }

    public string JobGroup
    {
        get
        {
            var attribute = GetType().GetCustomAttributes(typeof(QuartzJobAttribute), false)
                .FirstOrDefault() as QuartzJobAttribute;
            return attribute?.JobGroup ?? "Default";
        }
    }

    public string Description
    {
        get
        {
            var attribute = GetType().GetCustomAttributes(typeof(QuartzJobAttribute), false)
                .FirstOrDefault() as QuartzJobAttribute;
            return attribute?.Description ?? $"Job {GetType().Name}";
        }
    }

    public async Task Execute(IJobExecutionContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (context.JobDetail == null)
        {
            throw new InvalidOperationException("JobDetail is null in JobExecutionContext");
        }

        var jobKey = context.JobDetail.Key;
        var startTime = DateTime.UtcNow;
        
        if (Logger == null)
        {
            throw new InvalidOperationException($"Logger is null for job {jobKey.Name} in group {jobKey.Group}");
        }

        if (_context == null)
        {
            throw new InvalidOperationException($"Database context is null for job {jobKey.Name} in group {jobKey.Group}");
        }

        try
        {
            Logger.LogInformation("Executing job {JobName} in group {JobGroup}", JobName, JobGroup);
            await ExecuteJobAsync(context);
            Logger.LogInformation("Successfully executed job {JobName} in group {JobGroup}", JobName, JobGroup);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error while executing job {JobName} in group {JobGroup}", JobName, JobGroup);
            throw;
        }
    }
    
    protected abstract Task ExecuteJobAsync(IJobExecutionContext context);
}