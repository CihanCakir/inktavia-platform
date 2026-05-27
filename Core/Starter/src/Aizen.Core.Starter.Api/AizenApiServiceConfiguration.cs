using Aizen.Core.Cache.Extension;
using Aizen.Core.Configuration;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.InfoAccessor.Extensions;
using Aizen.Core.Infrastructure.CQRS.Extension;
using Aizen.Core.IOC.Extension;
using Aizen.Core.Messagebus.Extensions;
using Aizen.Core.RemoteCall.Extensions;
using Aizen.Core.Starter.Abstraction;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Aizen.Core.Starter.Api;

public class AizenApiServiceConfiguration : IAizenServiceConfiguration
{
    public AizenAppInfo AppInfo { get; }

    public AizenApiServiceConfiguration(AizenAppInfo appInfo)
    {
        AppInfo = appInfo;
    }
    
    public void Configure(IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        AizenConfiguration.Configuration = configuration;
        services.AddHttpContextAccessor();
        services.AddAizenApi(configuration);

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
        services.AddAizenCache(configuration);
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
    }
}