using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nuclea.Common.Business.Interfaces;
using Nuclea.Data;
using Nuclea.Quartz.Business.Extensions;
using Nuclea.Quartz.Business.Interfaces;
using Nuclea.Quartz.Business.Jobs;
using Nuclea.Quartz.Business.Services;
using Quartz;
using SilkierQuartz;

namespace Nuclea.Quartz.Business;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddBusinessServices(
            IConfiguration configuration)
        {
            services.Scan(scan => scan
                .FromAssemblies(AppDomain.CurrentDomain.GetAssemblies())
                .AddClasses(classes => classes.AssignableTo<IScopedService>())
                .AsImplementedInterfaces()
                .WithScopedLifetime()
                .AddClasses(classes => classes.AssignableTo<ISingletonService>())
                .AsImplementedInterfaces()
                .WithSingletonLifetime()
                .AddClasses(classes => classes.AssignableTo<ITransientService>())
                .AsImplementedInterfaces()
                .WithTransientLifetime());

            #region Core

            services.AddHttpClient();
            services.AddHttpContextAccessor();

            #endregion

            #region Quartz

            services.AddScoped<ExampleJob>();

            services.AddQuartz(q =>
            {
                q.SchedulerId = "AUTO";
                q.SchedulerName = "NucleaScheduler";

                q.UseSimpleTypeLoader();
                q.UseInMemoryStore();
                q.UseDefaultThreadPool(tp => { tp.MaxConcurrency = 10; });

                // Register jobs here
                q.AddQuartzJob<ExampleJob>("0 1 * * *");
            });

            services.AddQuartzHostedService(q =>
            {
                q.WaitForJobsToComplete = true;
                q.AwaitApplicationStarted = true;
            });

            services.TryAddSingleton<IQuartzSchedulerService, QuartzSchedulerService>();

            #endregion

            return services;
        }

        public IServiceCollection AddPersistence(IConfiguration configuration)
        {
            services.AddDbContext<NucleaDataContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("ConnectionString"));
            });

            return services;
        }

        public IServiceCollection AddProjectLocalization()
        {
            services.AddLocalization(o => { o.ResourcesPath = "Resources"; });

            services.Configure<RequestLocalizationOptions>(options =>
            {
                List<CultureInfo> supportedCultures = new List<CultureInfo>
                {
                    new CultureInfo("tr")
                };
                options.DefaultRequestCulture = new RequestCulture("tr");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
            });

            return services;
        }

        public IServiceCollection AddQuartzUI()
        {
            services.AddRazorPages();
            services.AddSilkierQuartz(options =>
                {
                    options.VirtualPathRoot = "/quartz";
                    options.UseLocalTime = true;
                    options.DefaultDateFormat = "yyyy-MM-dd";
                    options.DefaultTimeFormat = "HH:mm:ss";
                },
                auth =>
                {
                    auth.AccessRequirement = SilkierQuartzAuthenticationOptions.SimpleAccessRequirement.AllowAnonymous;
                });

            return services;
        }
    }
}