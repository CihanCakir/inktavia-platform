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
app.UseRateLimiter();

app.Run();
