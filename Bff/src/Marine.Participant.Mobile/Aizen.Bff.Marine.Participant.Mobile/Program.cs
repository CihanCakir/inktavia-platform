using Aizen.Bff.Marine.Participant.Mobile.Application;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Authorization;
using Aizen.Bff.Marine.Participant.Mobile.Extensions;
using Aizen.Bff.Marine.Participant.Mobile.Realtime;
using Aizen.Core.Cache.Extension;
using Aizen.Core.Realtime.Abstraction.Interfaces;
using Aizen.Core.Realtime.Extensions;
using Aizen.Core.Starter;
using Aizen.Core.Starter.Bff;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.RateLimiting;

var builder = AizenApplicationBuilder.CreateBuilder(new AizenAppInfo
{
    Name = "MarineMobileBff",
    Type = AppType.Bff,
    // BE-MO9b — the mobile BFF now hosts the realtime edge (owner notification bell). Without AppType.Worker in
    // TypeInclude, AddAizenMessagebus sets AddConsumer=false and the single realtime consumer never runs — the
    // NotificationSentMessage would be published to RabbitMQ but nobody in this process would bridge it to the hub.
    TypeInclude = { AppType.Worker }
}, args);

// Application services (Keycloak options, service token, participant context/identity holder, outgoing auth
// handler, Foundation Refit remote calls) + inbound Keycloak authentication + participant policies.
builder.Services
    .AddMarineMobileBffApplication(builder.Configuration)
    .AddMarineMobileAuthentication(builder.Configuration)
    .AddMarineMobileAuthorization();

// ── Distributed cache (Redis) ────────────────────────────────────────────────
// AppType.Bff does not register the cache the way AppType.Api does. Register it explicitly so
// IAizenDistributedCache is resolvable for handlers that need it.
builder.Services.AddAizenCache(builder.Configuration);

// ── Forwarded Headers (real client IP behind gateway/proxy) ───────────────────
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// ── CORS (mobile / SPA client → BFF) ──────────────────────────────────────────
// The mobile client calls the BFF cross-origin. Origins come from config (Cors:AllowedOrigins);
// falls back to the local mobile dev origin so local dev works out of the box.
//
// Registered under the shared name so the BFF pipeline can apply it BEFORE authentication.
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (corsOrigins is null || corsOrigins.Length == 0)
    corsOrigins = new[] { "http://localhost:19006" };
builder.Services.AddCors(options =>
{
    options.AddPolicy(AizenBffCors.PolicyName, policy => policy
        .WithOrigins(corsOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

// ── Realtime edge (ADR: BFF-hosted, on Aizen.Core.Realtime, modules publish-only) ─────────────
// BE-MO9b — the owner notification bell. The hub lives on the BFF (the mobile client only authenticates against the
// BFF) and the group a connection joins is decided server-side from the resolved participant id. Mirrors the
// admin-notification pair: one per-recipient group + one canonical NotificationSentMessage event.
//
// Redis backplane is REQUIRED for Kubernetes multi-replica: a RabbitMQ consumer on pod B pushes to pod B's hub
// context, but the participant's WebSocket may be on pod A — without the backplane the frame is silently dropped.
// AddAizenRealtime reads Realtime:SignalR:* — the compose/k8s env sets UseRedisBackplane=true + RedisConnectionString
// (a SEPARATE Redis DB from the cache so a cache FLUSHDB cannot take realtime down). CORS stays the shared BFF policy
// applied before auth (above). Module-mapper auto-discovery stays OFF — this BFF supplies its own single mapper.
builder.Services.AddAizenRealtime(builder.Configuration, o => o.RegisterModuleMappers = false);

// The hub broadcasts to one group prefix ("mobile-notification:{id}") → one domain-key registration. (The socket
// manager routes a group broadcast to a hub by parsing the group-name prefix up to the first ':'.)
builder.Services.AddDomainHub<MobileRealtimeHub>("mobile-notification");
// Phase-2 — the same hub also serves per-SR live-trip groups (trip:{serviceRequestId}). The framework routes a
// group broadcast to a hub by its group PREFIX, so the hub must be registered under the "trip" domain key too.
builder.Services.AddDomainHub<MobileRealtimeHub>("trip");

// The single per-surface routing declaration: NotificationSentMessage → thin cost-free frame + the recipient group.
builder.Services.AddSingleton<IEventSocketMapper, MobileNotificationEventSocketMapper>();

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
    // A throttle must be an UNMISTAKABLE typed 429 — not a bodiless response that reads like the masked-OK
    // path. Emit the standard envelope (stable errorCode 42900 + Retry-After) so the client can tell
    // "slow down" apart from success or an upstream error.
    opts.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        if (context.Lease.TryGetMetadata(System.Threading.RateLimiting.MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
        await context.HttpContext.Response.WriteAsync(
            "{\"header\":{\"isSuccess\":false,\"errorCode\":42900,\"errorMessage\":\"Too many attempts. Please wait a moment and try again.\"},\"body\":null}",
            token);
    };
});

var app = builder.Build();

app.UseForwardedHeaders();
// CORS is applied inside the BFF pipeline (before authentication) — see AizenBffApplicationConfiguration.
app.UseRateLimiter();

// BE-MO9b — the owner notification bell hub. The mobile client connects here (only the BFF is publicly reachable).
app.MapHub<MobileRealtimeHub>("/hubs/notification");

app.Run();
