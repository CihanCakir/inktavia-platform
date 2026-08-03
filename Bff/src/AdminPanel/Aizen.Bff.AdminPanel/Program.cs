using Aizen.Bff.AdminPanel.Application;
using Aizen.Bff.AdminPanel.Extensions;
using Aizen.Bff.AdminPanel.Realtime;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Realtime.Abstraction.Interfaces;
using Aizen.Core.Realtime.Extensions;
using Aizen.Core.Starter;
using Aizen.Core.Starter.Bff;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = AizenApplicationBuilder.CreateBuilder(new AizenAppInfo
{
    Name = "AdminPanelBff",
    Type = AppType.Bff,
    // AppType.Worker enables the Aizen messagebus consumer host so AdminMessagingRealtimeConsumer actually runs
    // (bridges the module's MessagingMessageSentMessage bus event to this BFF's realtime hub). Same setup as
    // the MarineProvider BFF. Without it, AddAizenMessagebus sets AddConsumer=false and no consumer is hosted.
    TypeInclude = { AppType.Worker }
}, args);

builder.Services.AddAdminPanelBffApplication(builder.Configuration);

// BFF inbound authentication: Keycloak (full IdP), RS256 — mirrors the MarineProvider BFF exactly (no HS256 /
// SymmetricSecurityKey). The human Keycloak token arrives in the Authorization: Bearer header (like provider); the
// BFF→module Keycloak service token is injected outbound by AdminPanelBffAuthDelegatingHandler. X-Aizen-User-Token is gone.
builder.Services.AddAdminPanelAuthentication(builder.Configuration);

builder.Services.AddAuthorization(options =>
{
    // AdminPanelAccess: an authenticated Keycloak token carrying the "Admin" realm role
    // (realm_access.roles → ClaimTypes.Role via MapKeycloakRealmRoles). Non-admin tokens are rejected (403).
    options.AddPolicy("AdminPanelAccess", policy =>
        policy
            .RequireAuthenticatedUser()
            .RequireRole("Admin"));

    // Default fallback: all endpoints require authentication unless decorated with [AllowAnonymous].
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// ── Realtime edge (ADR: BFF-hosted, on Aizen.Core.Realtime, modules publish-only) ─────────────
// CORS for the browser SPA → BFF hub. Origins come from Realtime:SignalR:AllowedOrigins (the config
// standard from the ADR). Registered under the SHARED BFF policy name so the shared BFF pipeline
// (AizenBffApplicationConfiguration) applies it BEFORE authentication — otherwise the hub's credential-less
// OPTIONS preflight (the browser cannot attach a bearer) would be answered 401 and the socket never opens.
var realtimeOrigins = builder.Configuration.GetSection("Realtime:SignalR:AllowedOrigins").Get<string[]>();
if (realtimeOrigins is { Length: > 0 })
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(AizenBffCors.PolicyName, policy => policy
            .WithOrigins(realtimeOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            // SignalR negotiates with credentials; without this the WebSocket handshake is blocked by the browser.
            .AllowCredentials());
    });
}

// Shared framework plumbing (SignalR runtime + optional Redis backplane from Realtime:SignalR:* + ingress +
// socket manager). Keep module-mapper auto-discovery OFF: this BFF supplies its own mapper and must not pull
// module types into its container.
builder.Services.AddAizenRealtime(builder.Configuration, o => o.RegisterModuleMappers = false);
builder.Services.AddDomainHub<AdminMessagingHub>("admin-messaging");

// The only per-surface routing declaration (ADR layer-2). Singleton because the framework's
// RealtimeIngressService (which consumes the single IEventSocketMapper) is registered as a singleton.
builder.Services.AddSingleton<IEventSocketMapper, AdminMessagingEventSocketMapper>();

var app = builder.Build();

// The browser authenticates only against the BFF and connects only here — never to a module hub.
app.MapHub<AdminMessagingHub>("/hubs/admin-messaging");

app.Run();

