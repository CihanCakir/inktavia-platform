using Hangfire;
using Aizen.Core.Common.Abstraction.Enums;
using Aizen.Core.Common.Abstraction.Exception;
using Aizen.Core.Common.Abstraction.Helpers;
using Aizen.Core.InfoAccessor;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.IOC.Abstraction.Extensions;
using Aizen.Core.Scheduler.Abstraction.RecurringJob;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Core.Scheduler.Extensions
{
    public static class RecurringJobBuilderExtension
    {
        public static IServiceCollection AddAizenRecurringJob(this IServiceCollection services,
            Action<AizenSchedulerOptions> setupAction = null)
        {
            if (services == null)
            {
                throw new AizenException($"ServiceCollection: {nameof(services)} not found.");
            }

            services.AddAizenBaseScheduler(setupAction);

            var moduleDiscovery = AizenModuleAssemblyDiscovery.GetInstance();

            services.ToScan(scan => scan.FromAssemblies(moduleDiscovery.ModuleAssemblies)
                .AddClasses(x => x.AssignableTo(typeof(IAizenRecurringJob)))
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            return services;
        }

        public static void UseAizenRecurringJob(this IApplicationBuilder app)
        {
            app.UseBaseAizenScheduler();

            using (var scope = app.ApplicationServices.CreateScope())
            {
                var recurringJobs = scope.ServiceProvider.GetRequiredService<IEnumerable<IAizenRecurringJob>>();
                foreach (var job in recurringJobs)
                {
                    var infoAccessor = scope.ServiceProvider.GetRequiredService<IAizenInfoAccessor>();

                    var jobType = job.GetType();
                    var jobPrefix = job.GetType().Namespace.Split('.').Last();
                    var queueName =
                        $"{infoAccessor.AppInfoAccessor.AppInfo.Name}-{infoAccessor.AppInfoAccessor.AppInfo.Type}"
                            .ToLower();
                    RecurringJob.AddOrUpdate(
                        $"{infoAccessor.AppInfoAccessor.AppInfo.Name}-{jobPrefix}.{jobType.Name}",
                        () => job.ExecuteAsync(CancellationToken.None),
                        job.CronExpression,
                        TimeZoneInfo.Utc,
                        queueName
                    );
                }
            }
        }
    }
}