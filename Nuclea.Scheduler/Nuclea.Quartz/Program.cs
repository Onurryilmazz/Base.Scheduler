using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nuclea.Quartz.Business;
using Nuclea.Quartz.Business.Jobs;
using Nuclea.Quartz.Endpoints;
using Nuclea.Quartz.Middleware;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

#region Db Context

builder.Services.AddPersistence(builder.Configuration);

#endregion

#region Services

builder.Services.AddBusinessServices(builder.Configuration);
builder.Services.AddProjectLocalization();

#endregion

#region Authentication & Authorization

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

#endregion

#region Quartz UI (SilkierQuartz)

builder.Services.AddQuartzUI();

#endregion

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

var dashboardUsername = app.Configuration["DashboardUser:Username"] ?? "admin";
var dashboardPassword = app.Configuration["DashboardUser:Password"] ?? "password";
app.UseMiddleware<QuartzBasicAuthenticationMiddleware>(dashboardUsername, dashboardPassword);
app.UseAuthorization();

#region SilkierQuartz Dashboard

app.UseSilkierQuartz();

#endregion

app.MapRazorPages();
app.MapJobEndpoints();

#region Register Jobs After Application Starts

var schedulerFactory = app.Services.GetRequiredService<ISchedulerFactory>();
var scheduler = await schedulerFactory.GetScheduler();

app.Lifetime.ApplicationStarted.Register(async () =>
{
    try
    {
        await Task.Delay(TimeSpan.FromSeconds(2));

        if (!scheduler.IsStarted)
        {
            await scheduler.Start();
        }


        var existingJobKeys = await scheduler.GetJobKeys(Quartz.Impl.Matchers.GroupMatcher<JobKey>.AnyGroup());

        if (existingJobKeys.Count == 0)
        {
            var configuration = app.Services.GetRequiredService<IConfiguration>();

            async Task AddJobAsync<T>(string jobName, string groupName, string cronExpression) where T : IJob
            {
                try
                {
                    var jobKey = new JobKey(jobName, groupName);

                    var jobDetail = JobBuilder.Create<T>()
                        .WithIdentity(jobKey)
                        .WithDescription($"Job {jobName}")
                        .StoreDurably()
                        .Build();

                    var trigger = TriggerBuilder.Create()
                        .WithIdentity($"{jobName}_trigger", groupName)
                        .ForJob(jobKey)
                        .WithCronSchedule(cronExpression, x => x.InTimeZone(TimeZoneInfo.Local))
                        .Build();

                    await scheduler.ScheduleJob(jobDetail, trigger);
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            await AddJobAsync<ExampleJob>(
                "ExampleJob",
                "ExampleJob",
                configuration["CronExpSettings:ExampleJob"] ?? "0 1 * * *");

            var newJobKeys = await scheduler.GetJobKeys(Quartz.Impl.Matchers.GroupMatcher<JobKey>.AnyGroup());

            foreach (var jobKey in newJobKeys)
            {
                var triggers = await scheduler.GetTriggersOfJob(jobKey);
            }
        }
        else
        {
            foreach (var jobKey in existingJobKeys)
            {
                var triggers = await scheduler.GetTriggersOfJob(jobKey);
            }
        }
    }
    catch (Exception)
    {
        // ignored
    }
});


#endregion

app.MapGet("/", () => Results.Redirect("/quartz"));
app.MapGet("/health", () => Results.Ok("Healthy"));

app.Run();