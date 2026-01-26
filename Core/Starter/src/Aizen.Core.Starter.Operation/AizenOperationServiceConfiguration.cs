using Aizen.Core.Configuration;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Starter.Abstraction;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Aizen.Core.Scheduler.Extensions;
using Aizen.Core.InfoAccessor.Extensions;
using Aizen.Core.Infrastructure.CQRS.Extention;
using Aizen.Core.IOC.Extention;
using Aizen.Core.Messagebus.Extentions;
using Aizen.Core.RemoteCall.Extentions;
namespace Aizen.Core.Starter.Operation;

public class AizenOperationServiceConfiguration : IAizenServiceConfiguration
{
    public AizenAppInfo AppInfo { get; }

    public AizenOperationServiceConfiguration(AizenAppInfo appInfo)
    {
        AppInfo = appInfo;
    }

    public void Configure(IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        AizenConfiguration.Configuration = configuration;

        services.AddHttpContextAccessor();
        if (this.AppInfo.TypeInclude.Contains(AppType.Api))
        {
            services.AddAizenApi(configuration);
        }

        // Ortak servisler
        services.AddAizenIOC(configuration);
        services.AddAizenCQRS(configuration);
        services.AddAizenMessagebus(configuration, settings =>
        {
            settings.AddConsumer = AppInfo.TypeInclude.Contains(AppType.Worker);
            settings.AddRequestClient = true;
        });
        services.AddAizenRemoteCall(configuration);
        services.AddAizenInfoAccessor(configuration);
        services.AddAizenValidation(configuration);


 

        // Scheduler spesifik servisler
        if (this.AppInfo.TypeInclude.Contains(AppType.Scheduler))
        {
            services.AddAizenRecurringJob();
            services.AddAizenBackgroundJob();
        }

        // Worker spesifik servisler
        if (this.AppInfo.TypeInclude.Contains(AppType.Worker))
        {
            services.AddRazorPages().AddRazorRuntimeCompilation();
            services.AddMvc().AddApplicationPart(GetType().Assembly);
        }

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
    }
}
