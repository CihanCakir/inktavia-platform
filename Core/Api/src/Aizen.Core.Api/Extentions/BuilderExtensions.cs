using System.Reflection;
using FluentValidation;
using LinqKit;
using MediatR;
using MediatR.Pipeline;
using Aizen.Core.Common.Abstraction.Exception;
using Aizen.Core.Common.Abstraction.Helpers;
using Aizen.Core.CQRS;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.CQRS.Decorator;
using Aizen.Core.CQRS.GenericHandler;
using Aizen.Core.CQRS.GenericMessage;
using Aizen.Core.CQRS.Pipeline;
using Aizen.Core.Domain;
using Aizen.Core.Infrastructure.Api.GenericApi;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Aizen.Core.Infrastructure.CQRS.Extention;

public static class BuilderExtensions
{
    public static IServiceCollection AddAizenApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers()
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();

            })
            .ConfigureApplicationPartManager(c =>
            {
                c.FeatureProviders.Add(new GenericRestControllerFeatureProvider());
            });

        return services;
    }

    private class GenericRestControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
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
                        Type closedControllerType = typeof(AizenGenericApi<>).MakeGenericType(entityType);
                        feature.Controllers.Add(closedControllerType.GetTypeInfo());
                    });
            });
        }
    }
}