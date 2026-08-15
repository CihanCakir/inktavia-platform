using Aizen.Bff.Marine.Web.Application;
using Aizen.Bff.Marine.Web.Application.Common.Authorization;
using Aizen.Bff.Marine.Web.Extensions;
using Aizen.Core.Cache.Extension;
using Aizen.Core.Starter;
using Aizen.Core.Starter.Bff;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.RateLimiting;

var builder = AizenApplicationBuilder.CreateBuilder(new AizenAppInfo
{
    Name = "MarineWebBff",
    Type = AppType.Bff
}, args);

// Application services (Keycloak options, service token, participant context/identity holder, outgoing auth
// handler) + inbound Keycloak authentication + web authorization policies.
builder.Services
    .AddMarineWebBffApplication(builder.Configuration)
    .AddMarineWebAuthentication(builder.Configuration)
    .AddMarineWebAuthorization();

// ── Distributed cache (Redis) ────────────────────────────────────────────────
// AppType.Bff does not register the cache the way AppType.Api does. Register it explicitly so
// IAizenDistributedCache is resolvable for handlers (cached public reads) added in W1.
builder.Services.AddAizenCache(builder.Configuration);

// ── Forwarded Headers (real client IP behind gateway/proxy) ───────────────────
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// ── CORS (website SPA → BFF) ──────────────────────────────────────────────────
// The public website calls the BFF cross-origin. Origins come from config (Cors:AllowedOrigins);
// falls back to the local web dev origin so local dev works out of the box.
// Registered under the shared name so the BFF pipeline applies it BEFORE authentication.
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (corsOrigins is null || corsOrigins.Length == 0)
    corsOrigins = new[] { "http://localhost:3000" };
builder.Services.AddCors(options =>
{
    options.AddPolicy(AizenBffCors.PolicyName, policy => policy
        .WithOrigins(corsOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

// ── Public-read IP rate limiting ("public-read-ip" policy, per client IP) ─────
// Used by the [AllowAnonymous] public content/reference endpoints added in W1.
var publicReadCfg = builder.Configuration.GetSection("RateLimiting:PublicRead");
var permitLimit = publicReadCfg.GetValue("PermitLimit", 120);
var windowSeconds = publicReadCfg.GetValue("WindowSeconds", 60);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("public-read-ip", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                SegmentsPerWindow = 6,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            }));
});

var app = builder.Build();

app.UseForwardedHeaders();
// CORS is applied inside the BFF pipeline (before authentication) — see AizenBffApplicationConfiguration.
app.UseRateLimiter();

app.Run();
