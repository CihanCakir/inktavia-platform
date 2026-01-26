using Aizen.Core.Common.Abstraction.Exception;
using Aizen.Core.Common.Abstraction.Helpers;
using Aizen.Core.IOC.Abstraction.Extensions;
using Aizen.Core.Scheduler.Abstraction;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Core.Scheduler.Extensions
{
    public static class BackgroundJobBuilderExtension
    {
        public static IServiceCollection AddAizenBackgroundJob(
            this IServiceCollection services,
            Action<AizenSchedulerOptions> setupAction = null)
        {
            if (services == null)
            {
                throw new AizenException($"ServiceCollection: {nameof(services)} not found.");
            }

            services.AddAizenBaseScheduler(setupAction);

            services.AddScoped<IAizenBackgroundJobManager, AizenBackgroundJobManager>();

            var moduleDiscovery = AizenModuleAssemblyDiscovery.GetInstance();
            services.ToScan(scan => scan.FromAssemblies(moduleDiscovery.ModuleAssemblies)
                .AddClasses(x => x.AssignableTo(typeof(IAizenBackgroundJob)))
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            return services;
        }

        public static void UseAizenBackgroundJob(
            this IApplicationBuilder app)
        {
            app.UseBaseAizenScheduler();
        }
    }
}
