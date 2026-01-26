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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Core.Infrastructure.CQRS.Extention;

public static class BuilderExtensions
{
    public static IServiceCollection AddAizenCQRS(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg=>cfg.RegisterServicesFromAssemblies(Assembly.GetExecutingAssembly()));
        services.AddScoped<IAizenCQRSProcessor, AizenCQRSProcessor>();

        AizenModuleAssemblyDiscovery.GetInstance().DomainAssemblies.ForEach(assembly =>
        {
            assembly.GetTypes()
                .Where(type =>
                    type is {IsClass: true, IsAbstract: false} && typeof(AizenEntity).IsAssignableFrom(type))
                .ForEach(type =>
                {
                    var genericTypeDefinitionsForQuery = new[]
                    {
                        typeof(AizenGetEntityByIdQueryHandler<>),
                        typeof(AizenSearchEntityForListQueryHandler<>),
                        typeof(AizenSearchEntityForPagedQueryHandler<>),
                    };
                    genericTypeDefinitionsForQuery.ForEach(genericTypeDefinition =>
                    {
                        Type[] typeArguments = {type};
                        Type constructedType = genericTypeDefinition.MakeGenericType(typeArguments);
                        constructedType.GetInterfaces().ForEach(serviceType =>
                        {
                            services.AddScoped(serviceType, constructedType);
                        });
                    });
                    var genericTypeDefinitionsForCommand = new[]
                    {
                        typeof(AizenInsertEntityCommandHandler<>),
                        typeof(AizenUpdateEntityCommandHandler<>),
                        typeof(AizenDeleteEntityCommandHandler<>),
                    };
                    genericTypeDefinitionsForCommand.ForEach(genericTypeDefinition =>
                    {
                        Type[] typeArguments = {type};
                        Type constructedType = genericTypeDefinition.MakeGenericType(typeArguments);
                        constructedType.GetInterfaces().ForEach(serviceType =>
                        {
                            services.AddScoped(serviceType, constructedType);
                        });
                    });
                });
        });

        var applicationAssemblies = AizenModuleAssemblyDiscovery.GetInstance().ApplicationAssemblies;

        services.Scan(scan => scan.FromAssemblies(applicationAssemblies)
            .AddClasses(classes => classes.AssignableTo(typeof(IAizenRequestHandler<,>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.Scan(scan => scan.FromAssemblies(applicationAssemblies)
            .AddClasses(classes => classes.AssignableTo(typeof(IAizenQueryHandler<,>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.Scan(scan => scan.FromAssemblies(applicationAssemblies)
            .AddClasses(classes => classes.AssignableTo(typeof(IAizenCommandHandler<,>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.AddScoped(typeof(IRequestPreProcessor<>), typeof(AizenValidationRequestPreProcessor<>));
        services.TryDecorate(typeof(IRequestHandler<,>), typeof(AizenQueryHandlerDecorator<,>));
        services.TryDecorate(typeof(IRequestHandler<,>), typeof(AizenCommandHandlerDecorator<,>));
        services.TryDecorate(typeof(IRequestHandler<,>), typeof(AizenPolicyMessageHandlerDecorator<,>));

        return services;
    }
}