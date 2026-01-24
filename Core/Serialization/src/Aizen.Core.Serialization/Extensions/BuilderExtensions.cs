using Aizen.Core.Common.Abstraction.Exception;
using Aizen.Core.Serialization.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Aizen.Core.Serialization.JSON.Extensions
{
    public class AizenJSONSerializationOptions
    {
    }

    public static class BuilderExtensions
    {
        public static IServiceCollection AddAizenJSONSerialization(this IServiceCollection services,
            Action<AizenJSONSerializationOptions> setupAction = null)
        {
            if (services == null)
            {
                throw new AizenException($"ServiceCollection: {nameof(services)} not found.");
            }

            JsonConvert.DefaultSettings = () => new AizenSerializerSettings();
            foreach (var serviceType in typeof(AizenJSONSerializer).GetInterfaces())
            {
                services.AddScoped(serviceType, typeof(AizenJSONSerializer));
            }

            if (setupAction != null)
            {
                services.Configure(setupAction);
            }

            return services;
        }
    }
}
