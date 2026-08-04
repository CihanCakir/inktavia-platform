using Aizen.Bff.Marine.Participant.Mobile.Application;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Authorization;
using Aizen.Bff.Marine.Participant.Mobile.Extensions;
using Aizen.Core.Cache.Extension;
using Aizen.Core.Starter;
using Aizen.Core.Starter.Bff;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.RateLimiting;

var builder = AizenApplicationBuilder.CreateBuilder(new AizenAppInfo
{
    Name = "MarineMobileBff",
    Type = AppType.Bff
    // Foundation has no bus consumers (realtime/SignalR is a later phase), so AppType.Worker is NOT
    // included in TypeInclude — the BFF does not host MassTransit consumers yet.
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
app.UseRateLimiter();

app.Run();
