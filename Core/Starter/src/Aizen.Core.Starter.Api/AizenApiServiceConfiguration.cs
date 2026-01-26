using Aizen.Core.Cache.Extention;
using Aizen.Core.Configuration;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.InfoAccessor.Extensions;
using Aizen.Core.Infrastructure.CQRS.Extention;
using Aizen.Core.IOC.Extention;
using Aizen.Core.Messagebus.Extentions;
using Aizen.Core.RemoteCall.Extentions;
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