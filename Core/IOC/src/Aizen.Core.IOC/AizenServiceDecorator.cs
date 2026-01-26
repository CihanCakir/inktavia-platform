using System.Reflection;
using Elastic.Apm;

namespace Aizen.Core.IOC;

public class AizenServiceDecorator<TDecorated> : DispatchProxy
{
    private TDecorated _service;

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        var transaction = Agent.Tracer.CurrentTransaction;

        var friendlyTypeName = GetReadableTypeName(_service.GetType());
        var friendlyMethodName = GetReadableMethodName(targetMethod);
        var spanName = $"{friendlyTypeName}-{friendlyMethodName}";
        var span = transaction?.StartSpan(spanName, _service.GetType().Name);
        try
        {
            var result = targetMethod.Invoke(_service, args);

            return result;
        }
        finally
        {
            span?.End();
        }
    }

    public static TDecorated Create(TDecorated decorated)
    {
        object proxy = Create<TDecorated, AizenServiceDecorator<TDecorated>>();
        ((AizenServiceDecorator<TDecorated>)proxy).SetParameters(decorated);

        return (TDecorated)proxy;
    }

    private void SetParameters(TDecorated decorated)
    {
        _service = decorated ?? throw new ArgumentNullException(nameof(decorated));
    }

    public static string GetReadableTypeName(Type type)
    {
        if (type.IsGenericType)
        {
            var genericTypeDefinition = type.GetGenericTypeDefinition();
            var genericTypeArguments = type.GetGenericArguments();

            // Generic tür adını oluşturun
            var genericTypeName = $"{genericTypeDefinition.Name}<";
            genericTypeName += string.Join(",", genericTypeArguments.Select(GetReadableTypeName));
            genericTypeName += ">";

            return genericTypeName;
        }

        return type.Name;
    }

    public static string GetReadableMethodName(MethodInfo methodInfo)
    {
        var methodName = methodInfo.Name;
        if (methodInfo.IsGenericMethod)
        {
            var genericTypeArguments = methodInfo.GetGenericArguments();
            var genericTypeNames = string.Join(",", genericTypeArguments.Select(arg => arg.Name));
            methodName += $"<{genericTypeNames}>";
        }

        return methodName;
    }
}