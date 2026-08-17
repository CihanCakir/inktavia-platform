using Aizen.Core.Cache.Abstraction;
using Aizen.Core.Common.Abstraction.Exception;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Core.Cache.Extension
{

    public static class BuilderExtensions
    {
        public static IServiceCollection AddAizenCache(this IServiceCollection services, IConfiguration configuration)
        {
            if (services == null)
            {
                throw new AizenException($"ServiceCollection: {nameof(services)} not found.");
            }

            services.Configure<MemoryCacheOptions>(configuration.GetSection("MemoryCache"));
            services.Configure<RedisCacheOptions>(configuration.GetSection("DistributedCache"));
            Connection.RedisConnection = configuration["DistributedCache:Configuration"];

            services.AddSingleton<AizenMemoryCache>();
            services.AddSingleton<AizenDistributedCache>();

            services.AddSingleton(typeof(IMemoryCache), x => x.GetRequiredService<AizenMemoryCache>());
            services.AddSingleton(typeof(IAizenMemoryCache), x => x.GetRequiredService<AizenMemoryCache>());

            services.AddSingleton(typeof(IDistributedCache), x => x.GetRequiredService<AizenDistributedCache>());
            services.AddSingleton(typeof(IAizenDistributedCache), x => x.GetRequiredService<AizenDistributedCache>());

            // Unified IAizenCache facade — resolves to the distributed (Redis) cache so multi-replica
            // consumers share state. Previously unregistered, which made any consumer (e.g. the Messaging
            // MessageContentPolicyService, on the message-send path) fail to activate.
            services.AddSingleton(typeof(IAizenCache), x => x.GetRequiredService<AizenDistributedCache>());

            return services;
        }
    }

    public static class Connection
    {
        public static string RedisConnection;
    }
}
