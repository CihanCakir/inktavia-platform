using Aizen.Bff.MarineProvider.Application;
using Aizen.Bff.MarineProvider.Application.Common.Authorization;
using Aizen.Bff.MarineProvider.Extensions;
using Aizen.Core.Starter;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = AizenApplicationBuilder.CreateBuilder(new AizenAppInfo
{
    Name = "MarineProviderBff",
    Type = AppType.Bff
}, args);

// Application services (Keycloak options, admin client, service token, provider context/resolver,
// outgoing auth handler, Refit remote calls) + inbound Keycloak authentication + provider policies.
builder.Services
    .AddMarineProviderBffApplication(builder.Configuration)
    .AddMarineProviderAuthentication(builder.Configuration)
    .AddMarineProviderAuthorization();

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
const string ProviderWebCorsPolicy = "provider-web";
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (corsOrigins is null || corsOrigins.Length == 0)
    corsOrigins = new[] { "http://localhost:3002" };
builder.Services.AddCors(options =>
{
    options.AddPolicy(ProviderWebCorsPolicy, policy => policy
        .WithOrigins(corsOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod());
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
// CORS must run before the rate limiter so preflight (OPTIONS) requests are answered
// with the CORS headers instead of being consumed/rejected by the limiter.
app.UseCors(ProviderWebCorsPolicy);
app.UseRateLimiter();

app.Run();
