using Aizen.Bff.AdminPanel.Application;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Starter;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = AizenApplicationBuilder.CreateBuilder(new AizenAppInfo
{
    Name = "AdminPanelBff",
    Type = AppType.Bff
}, args);

builder.Services.AddAdminPanelBffApplication(builder.Configuration);

// BFF inbound Identity token authentication.
// React/Admin Web sends Identity JWT in X-Aizen-User-Token header.
// The Authorization header is NOT used for inbound BFF auth (it carries the BFF->module Keycloak service token).
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["TokenOption:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["TokenOption:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["TokenOption:SecurityKey"] ?? string.Empty)),
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                // Read Identity token exclusively from X-Aizen-User-Token header.
                // Do NOT read from Authorization header — that carries the Keycloak BFF service token.
                var header = ctx.Request.Headers["X-Aizen-User-Token"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(header) &&
                    header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    ctx.Token = header["Bearer ".Length..].Trim();
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    // AdminPanelAccess: requires authenticated Identity token AND the "Admin" role claim.
    // Identity tokens are issued by the Identity module with ClaimTypes.Role = "Admin" for admin users.
    // Customer, mobile, and participant-only tokens (lacking the Admin role) are rejected.
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

