using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Nuclea.Quartz.Business.Interfaces;

namespace Nuclea.Quartz.Endpoints;

public static class JobEndpoints
{
    public static void MapJobEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/jobs")
            .WithTags("Jobs");

        group.MapPost("/trigger/{jobName}/{jobGroup}", TriggerJob)
            .WithName("TriggerJob")
            .WithDescription("Manually trigger a job");

        group.MapGet("/list", GetJobs)
            .WithName("GetJobs")
            .WithDescription("List all registered jobs");
    }

    private static async Task<IResult> TriggerJob(
        string jobName,
        string jobGroup,
        IQuartzSchedulerService schedulerService)
    {
        try
        {
            if (!schedulerService.IsStarted)
            {
                await schedulerService.StartAsync();
            }
            
            var jobDetail = await schedulerService.GetJobDetailAsync(jobName, jobGroup);
            if (jobDetail == null)
            {
                var allJobs = await schedulerService.GetJobKeysAsync();
                var jobList = allJobs.Select(j => $"{j.Group}.{j.Name}").ToList();
                return Results.NotFound(new
                {
                    error = $"Job '{jobGroup}.{jobName}' not found",
                    availableJobs = jobList
                });
            }

            await schedulerService.TriggerJobAsync(jobName, jobGroup);
            return Results.Ok(new { message = $"Job '{jobName}' in group '{jobGroup}' triggered successfully" });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetJobs(IQuartzSchedulerService schedulerService)
    {
        try
        {
            if (!schedulerService.IsStarted)
            {
                await schedulerService.StartAsync();
            }

            var jobs = await schedulerService.GetJobKeysAsync();
            var jobList = jobs.Select(j => new { Name = j.Name, Group = j.Group }).ToList();
            return Results.Ok(jobList);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}
