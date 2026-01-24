using Aizen.Core.Configuration;
using LinqKit;
using Aizen.Core.Common.Abstraction.Helpers;
using Aizen.Core.Domain;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.InfoAccessor.Extensions;
using Aizen.Core.Infrastructure.CQRS.Extention;
using Aizen.Core.IOC.Extention;
using Aizen.Core.Messagebus.Extentions;
using Aizen.Core.RemoteCall.Extentions;
using Aizen.Core.Starter.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;

namespace Aizen.Core.Starter.Worker;

public class AizenWorkerServiceConfiguration : IAizenServiceConfiguration
{
    public AizenAppInfo AppInfo { get; }

    public AizenWorkerServiceConfiguration(AizenAppInfo appInfo)
    {
        AppInfo = appInfo;
    }

    public void Configure(IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        AizenConfiguration.Configuration = configuration;

        services.AddHttpContextAccessor();
        services.AddControllers();

        services.AddRazorPages().AddRazorRuntimeCompilation();
        services.AddMvc().AddApplicationPart(GetType().Assembly);

        services.AddAizenIOC(configuration);
        services.AddAizenCQRS(configuration);
        services.AddAizenMessagebus(configuration);

        services.AddAizenRemoteCall(configuration);
        services.AddAizenInfoAccessor(configuration);
        services.AddAizenValidation(configuration);

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
    }
}