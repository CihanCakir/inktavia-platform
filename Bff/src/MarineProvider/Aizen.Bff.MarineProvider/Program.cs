using Aizen.Bff.MarineProvider.Application;
using Aizen.Bff.MarineProvider.Application.Common.Authorization;
using Aizen.Bff.MarineProvider.Extensions;
using Aizen.Bff.MarineProvider.Realtime;
using Aizen.Core.Cache.Extension;
using Aizen.Core.Starter;
using Aizen.Core.Starter.Bff;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = AizenApplicationBuilder.CreateBuilder(new AizenAppInfo
{
    Name = "MarineProviderBff",
    Type = AppType.Bff,
    // The BFF must consume bus messages (ServiceRequestPublished, OfferAccepted) to bridge them to the provider
    // hub. Without AppType.Worker in TypeInclude, AddAizenMessagebus sets AddConsumer=false and MassTransit
    // never registers the consumers — the messages are published to RabbitMQ but nobody in this process listens.
    TypeInclude = { AppType.Worker }
}, args);

// Application services (Keycloak options, admin client, service token, provider context/resolver,
// outgoing auth handler, Refit remote calls) + inbound Keycloak authentication + provider policies.
builder.Services
    .AddMarineProviderBffApplication(builder.Configuration)
    .AddMarineProviderAuthentication(builder.Configuration)
    .AddMarineProviderAuthorization();

// ── Distributed cache (Redis) ────────────────────────────────────────────────
// AppType.Bff does not register the cache the way AppType.Api does, so IAizenDistributedCache was not resolvable
// here. CreateUploadSessionCommandHandler takes it (per-provider upload rate limiting), which meant Autofac could
// not construct the handler and EVERY document upload died with a 500 at the container door — a dependency added
// without a registration. Register it explicitly.
builder.Services.AddAizenCache(builder.Configuration);

// ── Forwarded Headers (real client IP behind gateway/proxy) ───────────────────
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// ── CORS (browser SPA → BFF) ──────────────────────────────────────────────────
// The provider web app runs on a different origin (e.g. http://localhost:3002) and calls
// the BFF cross-origin. Without CORS the browser blocks every XHR (public + authenticated),
// including OTP login and password recovery. Origins come from config (Cors:AllowedOrigins);
// falls back to the local provider web origin so local dev works out of the box.
//
// Registered under the shared name so the BFF pipeline can apply it BEFORE authentication.
// It used to be applied here, after Build(), which put the CORS middleware behind
// UseAuthorization(): the hub's preflight (an OPTIONS with no Authorization header — the browser cannot
// add one) was answered 401 and the SignalR connection never opened. Do not move it back.
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (corsOrigins is null || corsOrigins.Length == 0)
    corsOrigins = new[] { "http://localhost:3002" };
builder.Services.AddCors(options =>
{
    options.AddPolicy(AizenBffCors.PolicyName, policy => policy
        .WithOrigins(corsOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        // SignalR negotiates with credentials; without this the WebSocket handshake is blocked by the browser.
        .AllowCredentials());
});

// ── Realtime (SignalR) ────────────────────────────────────────────────────────
// The hub lives on the BFF, not on a module: the browser only ever authenticates against the BFF, and the group a
// connection joins is decided server-side from the resolved provider profile.
//
// Redis backplane is REQUIRED for Kubernetes multi-replica: without it, a RabbitMQ consumer on pod B pushes to
// pod B's hub context, but the provider's WebSocket is on pod A — the event is silently dropped. With the
// backplane, SignalR re-broadcasts across all pods. Uses a separate Redis DB from the cache so FLUSHDB on the
// cache cannot take realtime down.
var signalRBuilder = builder.Services.AddSignalR();
var signalRRedisConn = builder.Configuration["Realtime:SignalR:RedisConnectionString"];
if (!string.IsNullOrWhiteSpace(signalRRedisConn))
    signalRBuilder.AddStackExchangeRedis(signalRRedisConn);

// ── IP Rate Limiting (password recovery abuse protection) ─────────────────────
var rlConfig = builder.Configuration.GetSection("RateLimiting:PasswordRecovery");
builder.Services.AddRateLimiter(opts =>
{
    opts.AddFixedWindowLimiter("pwd-recovery-ip", limiter =>
    {
        limiter.PermitLimit          = rlConfig.GetValue<int>("PermitPerWindow", 10);
        limiter.Window               = TimeSpan.FromSeconds(rlConfig.GetValue<int>("WindowSeconds", 300));
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit           = 0;
    });
    opts.RejectionStatusCode = 429;
});

var app = builder.Build();

app.UseForwardedHeaders();
// CORS is applied inside the BFF pipeline (before authentication) — see AizenBffApplicationConfiguration.
// Calling UseCors() here would place it after UseAuthorization(), which 401s the hub's preflight.
app.UseRateLimiter();

app.MapHub<ProviderRealtimeHub>("/hubs/provider");

app.Run();
