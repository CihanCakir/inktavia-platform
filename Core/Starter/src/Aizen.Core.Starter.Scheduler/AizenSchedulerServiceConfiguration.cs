using Aizen.Core.Configuration;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.InfoAccessor.Extensions;
using Aizen.Core.Infrastructure.CQRS.Extention;
using Aizen.Core.IOC.Extention;
using Aizen.Core.Messagebus.Extentions;
using Aizen.Core.RemoteCall.Extentions;
using Aizen.Core.Scheduler.Extensions;
using Aizen.Core.Starter.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;

namespace Aizen.Core.Starter.Scheduler;

public class AizenSchedulerServiceConfiguration : IAizenServiceConfiguration
{
    public AizenAppInfo AppInfo { get; }

    public AizenSchedulerServiceConfiguration(AizenAppInfo appInfo)
    {
        AppInfo = appInfo;
    }

    public void Configure(IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        AizenConfiguration.Configuration = configuration;

        services.AddHttpContextAccessor();
        services.AddControllers();

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

        services.AddAizenRecurringJob();
        services.AddAizenBackgroundJob();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

    }
}