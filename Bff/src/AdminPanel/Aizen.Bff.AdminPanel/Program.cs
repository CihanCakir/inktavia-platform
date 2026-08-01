using Aizen.Bff.AdminPanel.Application;
using Aizen.Bff.AdminPanel.Extensions;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Starter;
using Microsoft.AspNetCore.Authorization;

var builder = AizenApplicationBuilder.CreateBuilder(new AizenAppInfo
{
    Name = "AdminPanelBff",
    Type = AppType.Bff
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

var app = builder.Build();

app.Run();

