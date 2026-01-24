using Aizen.Core.Starter.Abstraction;
using Ocelot.Claims.Middleware;
using Ocelot.DownstreamPathManipulation.Middleware;
using Ocelot.DownstreamRouteFinder.Middleware;
using Ocelot.DownstreamUrlCreator.Middleware;
using Ocelot.Errors.Middleware;
using Ocelot.Headers.Middleware;
using Ocelot.LoadBalancer.Middleware;
using Ocelot.Middleware;
using Ocelot.Multiplexer;
using Ocelot.QueryStrings.Middleware;
using Ocelot.Request.Middleware;
using Ocelot.Requester.Middleware;
using Ocelot.RequestId.Middleware;
using Ocelot.Security.Middleware;

namespace Aizen.Core.Starter.Gateway;

// public class AizenGatewayApplicationConfiguration : IAizenApplicationConfiguration
// {
//     public void Configure(AizenApplication app, IWebHostEnvironment env)
//     {
//         app.UseCors(x => x
//             .AllowAnyMethod()
//             .AllowAnyHeader()
//             .AllowCredentials()
//             .SetIsOriginAllowed(origin => true)
//         );
        
//         app.UseOcelot((builder, configuration) =>
//         {
//             _ = app.UseMiddleware<InitializeAccessorMiddleware>();
//             _ = app.UseMiddleware<IpMiddleware>();
//             _ = app.UseDownstreamContextMiddleware();
//             _ = builder.UseExceptionHandlerMiddleware();
//             _ = builder.UseMiddleware<AuthorizationMiddleware>();
//             _ = app.UseMiddleware<BlockedUsernamesMiddleware>();
//             _ = builder.UseMiddleware<CryptographyMiddleware>();
//             _ = builder.UseMiddleware<CustomResponseMiddleware>();
//             _ = builder.UseDownstreamRouteFinderMiddleware();
//             _ = builder.UseMiddleware<MultiplexingMiddleware>();
//             _ = builder.UseMiddleware<InitializeDownstreamAccessorMiddleware>();
//             // _ = builder.UseSecurityMiddleware();
//             _ = builder.UseHttpHeadersTransformationMiddleware();
//             _ = builder.UseDownstreamRequestInitialiser();
//             _ = builder.UseMiddleware<RoutePathMiddleware>();
//             _ = builder.UseMiddleware<ChannelMiddleware>();
//             // _ = builder.UseMiddleware<CryptographyMiddleware>();
//             // _ = builder.UseMiddleware<CustomResponseMiddleware>();
//             //_ = builder.UseMiddleware<MockServiceMiddleware>();
//             _ = builder.UseRequestIdMiddleware();
//             _ = builder.UseClaimsToClaimsMiddleware();
//             _ = builder.UseClaimsToHeadersMiddleware();
//             _ = builder.UseClaimsToQueryStringMiddleware();
//             _ = builder.UseClaimsToDownstreamPathMiddleware();
//             _ = builder.UseLoadBalancingMiddleware();
//             _ = builder.UseDownstreamUrlCreatorMiddleware();
//             // _ = builder.UseMiddleware<NexusOutputCacheMiddleware>();
//             _ = builder.UseHttpRequesterMiddleware();
            
//             // _ = builder.UseDownstreamRouteFinderMiddleware();
//             // _ = builder.UseMiddleware<InitializeAccessorMiddleware>();
//             // _ = app.UseMiddleware<ChannelMiddleware>();
//             // _ = app.UseMiddleware<SubscriptionMiddleware>();
//             // _ = app.UseDownstreamContextMiddleware();
//             // _ = builder.UseExceptionHandlerMiddleware();
//             // _ = builder.UseMiddleware<CustomResponseMiddleware>();
//             // _ = builder.UseMultiplexingMiddleware();
//             // _ = builder.UseHttpHeadersTransformationMiddleware();
//             // _ = builder.UseDownstreamRequestInitialiser();
//             // _ = builder.UseRequestIdMiddleware();
//             // _ = builder.UseClaimsToClaimsMiddleware();
//             // _ = builder.UseClaimsToHeadersMiddleware();
//             // _ = builder.UseClaimsToQueryStringMiddleware();
//             // _ = builder.UseClaimsToDownstreamPathMiddleware();
//             // _ = builder.UseLoadBalancingMiddleware();
//             // _ = builder.UseDownstreamUrlCreatorMiddleware();
//             // _ = builder.UseHttpRequesterMiddleware();
//         }).Wait();
//     }
// }

