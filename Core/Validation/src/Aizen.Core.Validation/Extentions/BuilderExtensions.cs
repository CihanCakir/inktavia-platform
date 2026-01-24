using FluentValidation;
using Aizen.Core.Common.Abstraction.Helpers;
using Aizen.Core.Validation.Abstraction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Core.Infrastructure.CQRS.Extention;

public static class BuilderExtensions
{
    public static IServiceCollection AddAizenValidation(this IServiceCollection services, IConfiguration configuration)
    {
        services.Scan(scanner =>
        {
            var openHandlerTypes = new[]
            {
                typeof(IValidator<>),
            };
            foreach (var openHandlerType in openHandlerTypes)
            {
                scanner
                    .FromAssemblies(AizenModuleAssemblyDiscovery.GetInstance().ApplicationAssemblies)
                    .AddClasses(classes => classes.AssignableTo(openHandlerType))
                    .AsImplementedInterfaces()
                    .WithScopedLifetime();
            }
        });

        return services;
    }
}