using Autofac.Core;
using Autofac.Core.Resolving.Pipeline;
using AutoMapper.Internal;
using FluentValidation;
using ImpromptuInterface;
using Aizen.Core.Cache.Abstraction;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Data.Mongo.Repository;
using Aizen.Core.EFCore;
using Aizen.Core.IOC.Abstraction.Service;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Core.UnitOfWork.Abstraction;

namespace Aizen.Core.IOC;

public class AizenServiceMiddleware : IResolveMiddleware
{
    public PipelinePhase Phase => PipelinePhase.Activation;

    private readonly Type[] serviceTypes =
    {
        typeof(IAizenUnitOfWork<>),
        typeof(IAizenUnitOfWork),
        typeof(IAizenRepository<>),
        typeof(IAizenMongoRepository<>),
        
        typeof(IAizenRemoteCall),
        
        typeof(IAizenServiceTransient),
        typeof(IAizenServiceScope),
        typeof(IAizenServiceSingleton),
        
        typeof(IAizenDistributedCache),
        typeof(IAizenMemoryCache),
        typeof(IAizenCQRSProcessor),
        typeof(IValidator<>)
    };

    public void Execute(ResolveRequestContext context, Action<ResolveRequestContext> next)
    {
        // Before Activation

        next(context);

        // After Activation

        if (context.Service is TypedService service)
        {
            var serviceType = service.ServiceType;
            if (serviceType.IsInterface &&
                serviceTypes.Any(x => IsAssignableToGenericType(serviceType, x) || serviceType.IsAssignableTo(x)))
            {
                var decoratedServiceType = typeof(AizenServiceDecorator<>).MakeGenericType(serviceType);
                var decoratedService = decoratedServiceType.GetStaticMethod("Create")
                    .Invoke(null, new[] {context.Instance});
                var decoratedServiceAsInterface = Impromptu.DynamicActLike(decoratedService, serviceType);
                context.Instance = decoratedServiceAsInterface;
            }
        }
    }

    private static bool IsAssignableToGenericType(Type givenType, Type genericType)
    {
        var interfaceTypes = givenType.GetInterfaces();

        foreach (var it in interfaceTypes)
        {
            if (it.IsGenericType && it.GetGenericTypeDefinition() == genericType)
                return true;
        }

        if (givenType.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
            return true;

        Type baseType = givenType.BaseType;
        if (baseType == null) return false;

        return IsAssignableToGenericType(baseType, genericType);
    }
}