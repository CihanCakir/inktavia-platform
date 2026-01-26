using System.Reflection;
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
using Aizen.Core.Starter.Api.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;


namespace Aizen.Core.Starter.Api;

public class AizenBffServiceConfiguration : IAizenServiceConfiguration
{
    public AizenAppInfo AppInfo { get; }

    public AizenBffServiceConfiguration(AizenAppInfo appInfo)
    {
        AppInfo = appInfo;
    }

    public void Configure(IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddHttpContextAccessor();

        services.AddControllers()
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            })
            .ConfigureApplicationPartManager(c =>
            {
                c.FeatureProviders.Add(new BffRestControllerFeatureProvider());
            });

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
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
    }

    private class BffRestControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            AizenModuleAssemblyDiscovery.GetInstance().DomainAssemblies.ForEach(assembly =>
            {
                assembly.GetTypes()
                    .Where(type =>
                        type is {IsClass: true, IsAbstract: false} && typeof(AizenEntity).IsAssignableFrom(type))
                    .ForEach(entityType =>
                    {
                        Type closedControllerType = typeof(AizenGenericBffApi<>).MakeGenericType(entityType);
                        feature.Controllers.Add(closedControllerType.GetTypeInfo());
                    });
            });
        }
    }
}