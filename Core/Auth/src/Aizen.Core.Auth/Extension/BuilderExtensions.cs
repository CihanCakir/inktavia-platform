using System.Text;
using System.Linq;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Aizen.Core.Auth;
using Aizen.Core.Auth.Abstraction;
using Aizen.Core.Auth.Extension;
using Aizen.Core.Common.Abstraction.Configuration;
using Aizen.Core.Common.Abstraction.Exception;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;

namespace Aizen.Core.Infrastructure.Auth.Extension
{
    public static class BuilderExtensions
    {
        /// <summary>
        /// Registers Keycloak JWT Bearer authentication and a global authorization policy
        /// that requires authenticated users on all endpoints by default.
        /// Controllers with [AllowAnonymous] remain publicly accessible.
        /// Use this in service layers that validate Keycloak tokens but do not manage identity entities.
        /// </summary>
        public static IServiceCollection AddAizenKeycloakAuth(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // FAZ14 (#30): NullIfUnset — "__FROM_ENV__" gibi doldurulmamış yer-tutucu = AYARLANMAMIŞ sayılır.
            // Aksi hâlde aşağıdaki !IsNullOrWhiteSpace(keycloakAuthority) kararı yer-tutucuyu "dolu" sanıp
            // Keycloak dalına girer ve o.Authority="__FROM_ENV__" ile JWKS keşfi çöker (herkes için 401; borç #53).
            var keycloakAuthority = AizenConfigPlaceholders.NullIfUnset(
                configuration["Keycloak:Authority"] ?? configuration["KEYCLOAK_AUTHORITY"]);

            var keycloakMetadataAddress = AizenConfigPlaceholders.NullIfUnset(
                configuration["Keycloak:MetadataAddress"] ?? configuration["KEYCLOAK_METADATA_ADDRESS"]);

            var keycloakAudience = AizenConfigPlaceholders.NullIfUnset(
                configuration["Keycloak:Audience"] ?? configuration["KEYCLOAK_AUDIENCE"]);

            var requireHttpsMetadataValue = configuration["Keycloak:RequireHttpsMetadata"]
                ?? configuration["KEYCLOAK_REQUIRE_HTTPS_METADATA"];

            var requireHttpsMetadata = bool.TryParse(requireHttpsMetadataValue, out var parsedBool) && parsedBool;

            // Only register JWT Bearer if it hasn't been registered already (e.g. by AddAizenAuth).
            // Registering the Bearer scheme twice causes a runtime InvalidOperationException.
            // Using JwtBearerHandler as the indicator is more precise than IAuthenticationSchemeProvider,
            // which can be registered by the framework or AddControllers() without JWT Bearer.
            if (!services.Any(d => d.ServiceType == typeof(JwtBearerHandler)))
            {
                services.AddAuthentication(o =>
                {
                    o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, o =>
                {
                    if (!string.IsNullOrWhiteSpace(keycloakAuthority))
                    {
                        o.Authority = keycloakAuthority;

                        if (!string.IsNullOrWhiteSpace(keycloakMetadataAddress))
                            o.MetadataAddress = keycloakMetadataAddress;

                        o.RequireHttpsMetadata = requireHttpsMetadata;

                        o.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidIssuer = keycloakAuthority,
                            ValidateAudience = !string.IsNullOrWhiteSpace(keycloakAudience),
                            ValidAudience = string.IsNullOrWhiteSpace(keycloakAudience) ? null : keycloakAudience,
                            ValidateLifetime = true,
                            RoleClaimType = ClaimTypes.Role,
                            NameClaimType = "preferred_username"
                        };

                        o.Events = new JwtBearerEvents
                        {
                            OnTokenValidated = ctx =>
                            {
                                var id = ctx.Principal?.Identity as ClaimsIdentity;
                                if (id is null)
                                    return System.Threading.Tasks.Task.CompletedTask;

                                // Read realm_access and resource_access from principal claims.
                                // Works with both JwtSecurityToken (.NET 7) and JsonWebToken (.NET 8/9+) — the default
                                // JsonWebTokenHandler makes ctx.SecurityToken a JsonWebToken, so casting it to
                                // JwtSecurityToken (the previous impl) returned null and silently skipped role flattening,
                                // causing [Authorize(Roles="Admin")] to 403 for valid Keycloak tokens.
                                var realmAccessJson = ctx.Principal?.FindFirst("realm_access")?.Value;
                                if (!string.IsNullOrWhiteSpace(realmAccessJson))
                                {
                                    var realmAccess = JObject.Parse(realmAccessJson);
                                    var roles = realmAccess["roles"]?.Select(t => t.ToString()).ToArray() ?? System.Array.Empty<string>();
                                    foreach (var r in roles)
                                        if (!id.HasClaim(ClaimTypes.Role, r))
                                            id.AddClaim(new Claim(ClaimTypes.Role, r));
                                }

                                var resourceAccessJson = ctx.Principal?.FindFirst("resource_access")?.Value;
                                if (!string.IsNullOrWhiteSpace(resourceAccessJson))
                                {
                                    var resourceAccess = JObject.Parse(resourceAccessJson);
                                    foreach (var clientProp in resourceAccess.Properties())
                                    {
                                        var clientRoles = resourceAccess[clientProp.Name]?["roles"]?.Select(t => t.ToString()) ?? Enumerable.Empty<string>();
                                        foreach (var cr in clientRoles)
                                            if (!id.HasClaim(ClaimTypes.Role, cr))
                                                id.AddClaim(new Claim(ClaimTypes.Role, cr));
                                    }
                                }

                                return System.Threading.Tasks.Task.CompletedTask;
                            }
                        };
                    }
                    else
                    {
                        o.RequireHttpsMetadata = false;
                        o.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidIssuer = configuration["TokenOption:Issuer"],
                            ValidAudience = configuration["TokenOption:Audience"],
                            IssuerSigningKey = new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(configuration["TokenOption:SecurityKey"] ?? string.Empty)),
                            ValidateIssuerSigningKey = true,
                            ValidateIssuer = true,
                            ValidateAudience = true,
                        };
                    }
                });
            }

            // Require authenticated user on all controller endpoints by default.
            // Endpoints decorated with [AllowAnonymous] remain publicly accessible.
            services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });

            return services;
        }


        public static IServiceCollection AddAizenAuth<
        TUser, TRole,
        TUserClaim, TUserRole, TUserLogin, TRoleClaim, TUserToken,
        TContext>(
        this IServiceCollection services, WebApplicationBuilder builder)
        where TUser      : IdentityUser<long>
        where TRole      : IdentityRole<long>
        where TUserClaim : IdentityUserClaim<long>
        where TUserRole  : IdentityUserRole<long>
        where TUserLogin : IdentityUserLogin<long>
        where TRoleClaim : IdentityRoleClaim<long>
        where TUserToken : IdentityUserToken<long>
        where TContext   : IdentityDbContext<TUser, TRole, long, TUserClaim, TUserRole, TUserLogin, TRoleClaim, TUserToken>
        {
            if (services == null)
                throw new AizenException($"ServiceCollection: {nameof(services)} not found.");

            builder.Services.Configure<TokenSettings>(builder.Configuration.GetSection("TokenOption"));

            services.AddScoped<IAizenTokenHelper, AizenTokenHelper>();
            services.AddScoped<IAizenUserService, AizenUserService>();

            builder.Services
                .AddIdentity<TUser, TRole>(opts =>
                {
                    opts.User.RequireUniqueEmail = true;
                    opts.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                    opts.Password.RequiredLength = 6;
                    opts.Password.RequireNonAlphanumeric = false;
                    opts.Password.RequireLowercase = true;
                    opts.Password.RequireUppercase = true;
                    opts.Password.RequireDigit = true;
                })
                .AddErrorDescriber<AizenCustomIdentityErrorDescriber>()
                .AddEntityFrameworkStores<TContext>()
                .AddDefaultTokenProviders();

            services.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, o =>
            {
                // FAZ14 (#30): doldurulmamış yer-tutucu = AYARLANMAMIŞ (aşağıdaki Keycloak/simetrik dal kararı için).
                var keycloakAuthority = AizenConfigPlaceholders.NullIfUnset(
                    builder.Configuration["Keycloak:Authority"] ?? builder.Configuration["KEYCLOAK_AUTHORITY"]);

                var keycloakMetadataAddress = AizenConfigPlaceholders.NullIfUnset(
                    builder.Configuration["Keycloak:MetadataAddress"] ?? builder.Configuration["KEYCLOAK_METADATA_ADDRESS"]);

                var keycloakAudience = AizenConfigPlaceholders.NullIfUnset(
                    builder.Configuration["Keycloak:Audience"] ?? builder.Configuration["KEYCLOAK_AUDIENCE"]);

                var requireHttpsMetadataValue = builder.Configuration["Keycloak:RequireHttpsMetadata"]
                    ?? builder.Configuration["KEYCLOAK_REQUIRE_HTTPS_METADATA"];

                var requireHttpsMetadata = bool.TryParse(requireHttpsMetadataValue, out var parsedBool) && parsedBool;

                if (!string.IsNullOrWhiteSpace(keycloakAuthority))
                {
                    o.Authority = keycloakAuthority;

                    if (!string.IsNullOrWhiteSpace(keycloakMetadataAddress))
                        o.MetadataAddress = keycloakMetadataAddress;

                    o.RequireHttpsMetadata = requireHttpsMetadata;

                    o.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = keycloakAuthority,
                        ValidateAudience = !string.IsNullOrWhiteSpace(keycloakAudience),
                        ValidAudience = string.IsNullOrWhiteSpace(keycloakAudience) ? null : keycloakAudience,
                        ValidateLifetime = true,
                        RoleClaimType = ClaimTypes.Role,
                        NameClaimType = "preferred_username"
                    };

                    o.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = ctx =>
                        {
                            var id = ctx.Principal?.Identity as ClaimsIdentity;
                            if (id is null)
                                return System.Threading.Tasks.Task.CompletedTask;

                            // Read realm_access and resource_access from principal claims.
                            // Works with both JwtSecurityToken (.NET 7) and JsonWebToken (.NET 8/9+).
                            var realmAccessJson = ctx.Principal?.FindFirst("realm_access")?.Value;
                            if (!string.IsNullOrWhiteSpace(realmAccessJson))
                            {
                                var realmAccess = JObject.Parse(realmAccessJson);
                                var roles = realmAccess["roles"]?.Select(t => t.ToString()).ToArray() ?? System.Array.Empty<string>();
                                foreach (var r in roles)
                                    id.AddClaim(new Claim(ClaimTypes.Role, r));
                            }

                            var resourceAccessJson = ctx.Principal?.FindFirst("resource_access")?.Value;
                            if (!string.IsNullOrWhiteSpace(resourceAccessJson))
                            {
                                var resourceAccess = JObject.Parse(resourceAccessJson);
                                foreach (var clientProp in resourceAccess.Properties())
                                {
                                    var clientRoles = resourceAccess[clientProp.Name]?["roles"]?.Select(t => t.ToString()) ?? Enumerable.Empty<string>();
                                    foreach (var cr in clientRoles)
                                        id.AddClaim(new Claim(ClaimTypes.Role, cr));
                                }
                            }

                            return System.Threading.Tasks.Task.CompletedTask;
                        }
                    };
                }
                else
                {
                    // Fallback to symmetric key configuration (existing behavior)
                    o.RequireHttpsMetadata = false;
                    o.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidIssuer = builder.Configuration["TokenOption:Issuer"],
                        ValidAudience = builder.Configuration["TokenOption:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(builder.Configuration["TokenOption:SecurityKey"])),
                        ValidateIssuerSigningKey = true,
                        ValidateIssuer = true,
                        ValidateAudience = true,
                    };
                }
            });

            return services;
        }
      
    
      
        public static IServiceCollection AddAizenAuth<TUser, TRole, TContext>(
            this IServiceCollection services, WebApplicationBuilder builder)
            where TUser : IdentityUser<long>
            where TRole : IdentityRole<long>
            where TContext : IdentityDbContext<TUser, TRole, long>
        {

            if (services == null)
            {
                throw new AizenException($"ServiceCollection: {nameof(services)} not found.");
            }
            builder.Services.Configure<TokenSettings>(builder.Configuration.GetSection("TokenOption"));

            services.AddScoped<IAizenTokenHelper, AizenTokenHelper>();

            services.AddScoped<IAizenUserService, AizenUserService>();

            services.AddScoped(typeof(UserManager<>));

            services.AddScoped(typeof(PasswordValidator<>));

            services.AddScoped(typeof(SignInManager<>));

            builder.Services.AddIdentity<TUser, TRole>(opts =>
                {
                    opts.User.RequireUniqueEmail = true; // E-posta adreslerinin benzersiz olması gerekiyor
                    opts.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+"; // Kullanıcı adında e-posta için gerekli olan karakterler kullanılabilir
                    opts.Password.RequiredLength = 6; // Parolanın en az 6 karakterden oluşması gerekiyor
                    opts.Password.RequireNonAlphanumeric = false; // Parolada sembol kullanımı zorunlu değil
                    opts.Password.RequireLowercase = true; // Parolada en az bir küçük harf bulunmalı
                    opts.Password.RequireUppercase = true; // Parolada en az bir büyük harf bulunmalı
                    opts.Password.RequireDigit = true; // Parolada en az bir rakam bulunmalı
                })
                 .AddErrorDescriber<AizenCustomIdentityErrorDescriber>()
                 .AddEntityFrameworkStores<TContext>().AddDefaultTokenProviders();

            services.AddAuthentication(options =>
                        {
                            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                        }).AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                            {
                                // FAZ14 (#30): doldurulmamış yer-tutucu = AYARLANMAMIŞ (Keycloak/simetrik dal kararı için).
                                var keycloakAuthority = AizenConfigPlaceholders.NullIfUnset(
                                    builder.Configuration["Keycloak:Authority"] ?? builder.Configuration["KEYCLOAK_AUTHORITY"]);

                                var keycloakMetadataAddress = AizenConfigPlaceholders.NullIfUnset(
                                    builder.Configuration["Keycloak:MetadataAddress"] ?? builder.Configuration["KEYCLOAK_METADATA_ADDRESS"]);

                                var keycloakAudience = AizenConfigPlaceholders.NullIfUnset(
                                    builder.Configuration["Keycloak:Audience"] ?? builder.Configuration["KEYCLOAK_AUDIENCE"]);

                                var requireHttpsMetadataValue = builder.Configuration["Keycloak:RequireHttpsMetadata"]
                                    ?? builder.Configuration["KEYCLOAK_REQUIRE_HTTPS_METADATA"];

                                var requireHttpsMetadata = bool.TryParse(requireHttpsMetadataValue, out var parsedBool) && parsedBool;

                                if (!string.IsNullOrWhiteSpace(keycloakAuthority))
                                {
                                    options.Authority = keycloakAuthority;

                                    if (!string.IsNullOrWhiteSpace(keycloakMetadataAddress))
                                        options.MetadataAddress = keycloakMetadataAddress;

                                    options.RequireHttpsMetadata = requireHttpsMetadata;

                                    options.TokenValidationParameters = new TokenValidationParameters
                                    {
                                        ValidateIssuer = true,
                                        ValidIssuer = keycloakAuthority,
                                        ValidateAudience = !string.IsNullOrWhiteSpace(keycloakAudience),
                                        ValidAudience = string.IsNullOrWhiteSpace(keycloakAudience) ? null : keycloakAudience,
                                        ValidateLifetime = true,
                                        RoleClaimType = ClaimTypes.Role,
                                        NameClaimType = "preferred_username"
                                    };

                                    options.Events = new JwtBearerEvents
                                    {
                                        OnTokenValidated = ctx =>
                                        {
                                            var id = ctx.Principal?.Identity as ClaimsIdentity;
                                            if (id is null)
                                                return System.Threading.Tasks.Task.CompletedTask;

                                            // Read from principal claims — works with both JwtSecurityToken (.NET 7) and
                                            // JsonWebToken (.NET 8/9+). See the AddAizenKeycloakAuth note above.
                                            var realmAccessJson = ctx.Principal?.FindFirst("realm_access")?.Value;
                                            if (!string.IsNullOrWhiteSpace(realmAccessJson))
                                            {
                                                var realmAccess = JObject.Parse(realmAccessJson);
                                                var roles = realmAccess["roles"]?.Select(t => t.ToString()).ToArray() ?? System.Array.Empty<string>();
                                                foreach (var r in roles)
                                                    if (!id.HasClaim(ClaimTypes.Role, r))
                                                        id.AddClaim(new Claim(ClaimTypes.Role, r));
                                            }

                                            var resourceAccessJson = ctx.Principal?.FindFirst("resource_access")?.Value;
                                            if (!string.IsNullOrWhiteSpace(resourceAccessJson))
                                            {
                                                var resourceAccess = JObject.Parse(resourceAccessJson);
                                                foreach (var clientProp in resourceAccess.Properties())
                                                {
                                                    var clientRoles = resourceAccess[clientProp.Name]?["roles"]?.Select(t => t.ToString()) ?? Enumerable.Empty<string>();
                                                    foreach (var cr in clientRoles)
                                                        if (!id.HasClaim(ClaimTypes.Role, cr))
                                                            id.AddClaim(new Claim(ClaimTypes.Role, cr));
                                                }
                                            }

                                            return System.Threading.Tasks.Task.CompletedTask;
                                        }
                                    };
                                }
                                else
                                {
                                    options.RequireHttpsMetadata = false;
                                    options.TokenValidationParameters = new TokenValidationParameters
                                    {
                                        ValidIssuer = builder.Configuration["TokenOption:Issuer"],
                                        ValidAudience = builder.Configuration["TokenOption:Audience"],
                                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["TokenOption:SecurityKey"])),

                                        ValidateIssuerSigningKey = true,
                                        ValidateIssuer = true,
                                        ValidateAudience = true,

                                    };
                                }


                            });



            return services;
        }
    }
}