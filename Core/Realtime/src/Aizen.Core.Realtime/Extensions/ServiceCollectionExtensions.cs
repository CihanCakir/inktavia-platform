using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Aizen.Core.Realtime.Abstraction.Interfaces;
using Aizen.Core.Realtime.Domain;
using Aizen.Core.Realtime.Filters;
using Aizen.Core.Realtime.Guards;
using Aizen.Core.Realtime.Services;
using Aizen.Core.Realtime.Middleware;

namespace Aizen.Core.Realtime.Extensions
{
    public class RealtimeRegistrationOptions
    {
        public bool AddSignalR { get; set; } = true;
        public bool RegisterModuleMappers { get; set; } = true;
        public bool RegisterModuleAuthorizers { get; set; } = true;
        public bool RegisterDomainRegistrars { get; set; } = true;
        // If true, AddAizenRealtime will also call endpoints.MapHub for built-in core hubs via MapAizenCoreHubs helper
        // Default false: modules must map their own hubs.
        public bool MapCoreHubsAutomatically { get; set; } = false;
    }

    public class SignalRSettings
    {
        public bool UseRedisBackplane { get; set; } = false;
        public string? RedisConnectionString { get; set; }
        public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
        public int KeepAliveIntervalSeconds { get; set; } = 15;
        public int ClientTimeoutSeconds { get; set; } = 30;
        public long? MaximumReceiveMessageSize { get; set; } = null;
        public bool EnableDetailedErrors { get; set; } = false;
        public int StreamBufferCapacity { get; set; } = 10;
        public string HubPath { get; set; } = "/hubs/activity";
    }

    public static partial class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers Realtime core services and SignalR runtime options.
        /// Does NOT map hub endpoints: modules should map their own hubs in Program.cs.
        /// Call this BEFORE AddAizenMessagebus if you want discovery to include Realtime-provided types.
        /// </summary>
        public static IServiceCollection AddAizenRealtime(this IServiceCollection services, IConfiguration configuration, Action<RealtimeRegistrationOptions>? setupAction = null)
        {
            var options = new RealtimeRegistrationOptions();
            setupAction?.Invoke(options);

            // Bind optional SignalR runtime settings
            var signalrSection = configuration.GetSection("Realtime:SignalR");
            var signalrSettings = signalrSection.Exists()
                ? signalrSection.Get<SignalRSettings>() ?? new SignalRSettings()
                : new SignalRSettings();

            // CORS (optional)
            if (signalrSettings.AllowedOrigins?.Length > 0)
            {
                services.AddCors(cors =>
                {
                    cors.AddPolicy("AizenRealtimeCors", policy =>
                    {
                        policy
                            .WithOrigins(signalrSettings.AllowedOrigins)
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials();
                    });
                });
            }

            // Add SignalR (hub types must be resolvable via DI; modules will map endpoints)
            if (options.AddSignalR)
            {
                var signalRBuilder = services.AddSignalR(hubOptions =>
                {
                    hubOptions.EnableDetailedErrors = signalrSettings.EnableDetailedErrors;
                    hubOptions.KeepAliveInterval = TimeSpan.FromSeconds(Math.Max(1, signalrSettings.KeepAliveIntervalSeconds));
                    hubOptions.ClientTimeoutInterval = TimeSpan.FromSeconds(Math.Max(10, signalrSettings.ClientTimeoutSeconds));
                    if (signalrSettings.MaximumReceiveMessageSize.HasValue)
                        hubOptions.MaximumReceiveMessageSize = signalrSettings.MaximumReceiveMessageSize.Value;
                    hubOptions.StreamBufferCapacity = Math.Max(1, signalrSettings.StreamBufferCapacity);
                });

                if (signalrSettings.UseRedisBackplane && !string.IsNullOrWhiteSpace(signalrSettings.RedisConnectionString))
                {
                    signalRBuilder.AddStackExchangeRedis(signalrSettings.RedisConnectionString, opts => { /* optional */ });
                    services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(signalrSettings.RedisConnectionString));
                }
            }

            // Core implementations (can be overridden by modules)
            services.AddSingleton<IRealtimePublisher, SignalRRealtimePublisher>();
            services.AddSingleton<ISocketManager, SignalRSocketManager>();
            services.AddSingleton<IRealtimeEventIngress, RealtimeIngressService>();

            // Guards and filters
            services.AddSingleton<IRateLimitGuard, InMemoryRateLimitGuard>();
            services.AddSingleton<IHubFilter, RealtimeHubFilter>();

            // Default user id provider (maps claims to Context.UserIdentifier)
            services.AddSingleton<IUserIdProvider, ClaimUserIdProvider>();

            // Hosted service runs IRealtimeDomainRegistrar.Register() at startup (if any are registered)
            if (options.RegisterDomainRegistrars)
                services.AddHostedService<DomainRegistrarHostedService>();

            // Best-effort module-type discovery (try to reuse project discovery used by Messagebus)
            try
            {
                var discoveryType = Type.GetType("Aizen.Core.ModuleDiscovery.AizenModuleAssemblyDiscovery, Aizen.Core.ModuleDiscovery")
                                    ?? Type.GetType("AizenModuleAssemblyDiscovery");
                object? discoveryInstance = null;
                if (discoveryType != null)
                {
                    var getInst = discoveryType.GetMethod("GetInstance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
                                 ?? discoveryType.GetMethod("get_Instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                    if (getInst != null)
                        discoveryInstance = getInst.Invoke(null, null);
                }

                if (discoveryInstance != null)
                {
                    var moduleAssembliesProp = discoveryInstance.GetType().GetProperty("ModuleAssemblies");
                    if (moduleAssembliesProp != null)
                    {
                        var assemblies = moduleAssembliesProp.GetValue(discoveryInstance) as System.Collections.IEnumerable;
                        if (assemblies != null)
                        {
                            var list = new List<System.Reflection.Assembly>();
                            foreach (var a in assemblies) if (a is System.Reflection.Assembly asm) list.Add(asm);

                            if (options.RegisterModuleMappers)
                            {
                                var mapperTypes = list.SelectMany(a => a.GetTypes())
                                    .Where(t => t.IsClass && !t.IsAbstract && typeof(IEventSocketMapper).IsAssignableFrom(t));
                                foreach (var mt in mapperTypes) services.AddScoped(typeof(IEventSocketMapper), mt);
                            }

                            var resolverTypes = list.SelectMany(a => a.GetTypes())
                                .Where(t => t.IsClass && !t.IsAbstract && typeof(IUserInfoResolver).IsAssignableFrom(t));
                            foreach (var rt in resolverTypes) services.AddScoped(typeof(IUserInfoResolver), rt);

                            if (options.RegisterModuleAuthorizers)
                            {
                                var authTypes = list.SelectMany(a => a.GetTypes())
                                    .Where(t => t.IsClass && !t.IsAbstract && typeof(IActivityAuthorizationService).IsAssignableFrom(t));
                                foreach (var at in authTypes) services.AddScoped(typeof(IActivityAuthorizationService), at);
                            }

                            if (options.RegisterDomainRegistrars)
                            {
                                var registrarTypes = list.SelectMany(a => a.GetTypes())
                                    .Where(t => t.IsClass && !t.IsAbstract && typeof(Aizen.Core.Realtime.Abstraction.Interfaces.IRealtimeDomainRegistrar).IsAssignableFrom(t));
                                foreach (var reg in registrarTypes) services.AddSingleton(typeof(Aizen.Core.Realtime.Abstraction.Interfaces.IRealtimeDomainRegistrar), reg);
                            }
                        }
                    }
                }
            }
            catch
            {
                // discovery best-effort; modules may register manually
            }

            // Bind SignalR settings for UseAizenRealtime to read HubPath / CORS config
            services.Configure<SignalRSettings>(signalrSection);

            return services;
        }

        /// <summary>
        /// Adds middleware used by Realtime pipeline (e.g. connection metadata).
        /// This does not map hub endpoints; modules should MapHub their own hubs.
        /// Call app.UseConnectionMetadata(); app.UseAuthentication(); app.UseAuthorization(); app.UseRouting(); then map hubs in module.
        /// </summary>
        public static IApplicationBuilder UseAizenRealtime(this IApplicationBuilder app)
        {
            // Use ConnectionMetadata middleware if available in Core/Realtime.Middleware
            // If middleware is implemented in core:
            try
            {
                app.UseConnectionMetadata(); // extension method from ConnectionMetadataMiddlewareExtensions
            }
            catch
            {
                // if middleware not available, modules can add their own
            }

            return app;
        }


        /// <summary>
        /// Register domain -> Hub mapping. Example: services.AddDomainHub&lt;ModuleActivityHub&gt;("activity");
        /// </summary>
        public static IServiceCollection AddDomainHub<THub>(this IServiceCollection services, string domainKey)
            where THub : Hub
        {
            DomainHubRegistry.RegisterDomainHub(domainKey, typeof(THub));
            return services;
        }
    }

    // Hosted service that invokes all IRealtimeDomainRegistrar implementations at startup
    internal class DomainRegistrarHostedService : IHostedService
    {
        private readonly IServiceProvider _sp;
        public DomainRegistrarHostedService(IServiceProvider sp) => _sp = sp;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _sp.CreateScope();
            var registrars = scope.ServiceProvider.GetServices<Aizen.Core.Realtime.Abstraction.Interfaces.IRealtimeDomainRegistrar>();
            foreach (var r in registrars)
            {
                try { r.Register(); } catch { /* swallow - registrar should manage errors */ }
            }
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    /// <summary>
    /// Default user id provider using JWT claim mapping ("sub" or name identifier).
    /// Register module override if needed.
    /// </summary>
    public class ClaimUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            var user = connection.User;
            if (user == null) return null;
            var claim = user.FindFirst("sub") ?? user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst("user_id");
            return claim?.Value;
        }
    }
}