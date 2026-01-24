using MassTransit;
using MassTransit.Transports;
using Aizen.Core.Messagebus.Abstraction.Settings;

namespace Aizen.Core.Messagebus.Extentions;

public class CustomEndpointNameFormatter : DefaultEndpointNameFormatter
{
    private readonly MessageBrokerQueueSettings _messageBrokerQueueSettings;

    public CustomEndpointNameFormatter(MessageBrokerQueueSettings messageBrokerQueueSettings)
    {
        _messageBrokerQueueSettings = messageBrokerQueueSettings;
    }
}

public class CustomMessageNameFormatter : IEntityNameFormatter
{
    private readonly IEntityNameFormatter _original;
    private readonly MessageBrokerQueueSettings _messageBrokerQueueSettings;

    public CustomMessageNameFormatter(IEntityNameFormatter original,
        MessageBrokerQueueSettings messageBrokerQueueSettings)
    {
        _original = original;
        _messageBrokerQueueSettings = messageBrokerQueueSettings;
    }

    public string FormatEntityName<T>()
    {
        try
        {
            var result = string.Empty;
            if (typeof(T).IsGenericType)
            {
                var genericTypeName = typeof(T).GetGenericTypeDefinition().Name.Replace("`1", "");
                var parameterTypeName = typeof(T).GetGenericArguments().First().Name;

                result = $"{parameterTypeName}.{genericTypeName}";
            }
            else
            {
                result = typeof(T).Name;
            }

            if (_messageBrokerQueueSettings.Debug)
            {
                result = $"{Environment.MachineName}.{result}";
            }

            return result;
        }
        catch (Exception)
        {
            return _original.FormatEntityName<T>();
        }
    }
}