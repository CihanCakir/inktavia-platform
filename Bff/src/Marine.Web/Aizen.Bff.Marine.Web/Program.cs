using Aizen.Bff.Marine.Web.Application;
using Aizen.Bff.Marine.Web.Application.Common.Authorization;
using Aizen.Bff.Marine.Web.Application.Common.Options;
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
    Type = AppType.Bff,
    // W2.2 — the BFF also hosts bus consumers (the Content revalidation bridge). AppType.Worker in TypeInclude
    // makes AddAizenMessagebus (in the BFF starter) register the entry-assembly consumers.
    TypeInclude = new List<AppType> { AppType.Worker },
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

// ── Public-read rate limiting ("public-read-ip" policy) ───────────────────────
// Used by the [AllowAnonymous] public content/reference endpoints.
//
// The real consumer is a SERVER-RENDERED Next.js site: it makes every request for every visitor from a HANDFUL of
// IPs, so a strict per-IP limit would throttle the whole website at modest traffic. So the same policy branches on
// a trusted-caller secret (mirrors the ecosystem's X-Aizen-Bff-Assertion shared-secret style):
//   • valid X-Aizen-Web-Caller secret → one shared, much-higher "trusted-web-caller" partition (server-to-server).
//   • everyone else                   → the per-IP sliding window, UNCHANGED (browsers / untrusted callers).
// The strict per-IP protection is preserved exactly; the trusted path is additive and fail-closed (an unset secret
// leaves every caller on the per-IP limit).
var publicReadCfg = builder.Configuration.GetSection("RateLimiting:PublicRead");
var permitLimit = publicReadCfg.GetValue("PermitLimit", 120);
var windowSeconds = publicReadCfg.GetValue("WindowSeconds", 60);

var publicCfg = builder.Configuration.GetSection(MarineWebPublicOptions.SectionName).Get<MarineWebPublicOptions>()
                ?? new MarineWebPublicOptions();
var trustedSecret = publicCfg.TrustedCallerSecret;
var trustedPermit = publicCfg.TrustedRateLimit.PermitLimit;
var trustedWindow = publicCfg.TrustedRateLimit.WindowSeconds;

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("public-read-ip", httpContext =>
    {
        // Trusted server-side caller → one shared partition, much higher limit (<= 0 ⇒ effectively unlimited).
        // The decision + constant-time compare live in the testable TrustedWebCaller helper (W2.1 / W5).
        if (TrustedWebCaller.IsTrusted(httpContext, trustedSecret))
        {
            if (trustedPermit <= 0)
                return RateLimitPartition.GetNoLimiter(TrustedWebCaller.TrustedPartitionKey);

            return RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey: TrustedWebCaller.TrustedPartitionKey,
                factory: _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = trustedPermit,
                    Window = TimeSpan.FromSeconds(trustedWindow),
                    SegmentsPerWindow = 6,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0,
                });
        }

        // Untrusted (browsers / anything without the secret) → per-IP sliding window, exactly as before.
        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? TrustedWebCaller.UnknownIpPartitionKey,
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                SegmentsPerWindow = 6,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            });
    });

    // M4 — WRITE surface (POST /web/contact): a per-IP limit STRICTER than public-read-ip (a few submits/min), and
    // NOT bypassed by the trusted-caller secret (a contact form is user-driven, so it partitions on the real caller
    // IP regardless). Config RateLimiting:ContactSubmit (defaults 5 / 60s).
    var contactCfg = builder.Configuration.GetSection("RateLimiting:ContactSubmit");
    var contactPermit = contactCfg.GetValue("PermitLimit", 5);
    var contactWindow = contactCfg.GetValue("WindowSeconds", 60);
    options.AddPolicy("contact-submit", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? TrustedWebCaller.UnknownIpPartitionKey,
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = contactPermit,
                Window = TimeSpan.FromSeconds(contactWindow),
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
