using Aizen.Core.Configuration;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Starter.Abstraction;
using Aizen.Core.Starter.Abstraction.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Aizen.Core.Scheduler.Extensions;
using Aizen.Core.InfoAccessor.Extensions;
using Aizen.Core.Infrastructure.CQRS.Extension;
using Aizen.Core.IOC.Extension;
using Aizen.Core.Messagebus.Extensions;
using Aizen.Core.RemoteCall.Extensions;
using Aizen.Core.Infrastructure.Auth.Extension;
using Microsoft.OpenApi.Models;
namespace Aizen.Core.Starter.Operation;

public class AizenOperationServiceConfiguration : IAizenServiceConfiguration
{
    public AizenAppInfo AppInfo { get; }

    public AizenOperationServiceConfiguration(AizenAppInfo appInfo)
    {
        AppInfo = appInfo;
    }

    public void Configure(IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        // FAZ14 (#30): doldurulmamış "__FROM_ENV__"/"__FROM_SECRET__" yer-tutucularıyla açılmayı reddet.
        // Development dışında suçlu TÜM anahtarları listeleyip fırlatır; Development'ta loglayıp devam eder.
        // Herhangi bir servis kablolamasından ÖNCE, hızlı-başarısızlık için ilk çağrı.
        AizenConfigurationPlaceholderGuard.EnsureNoUnfilledPlaceholders(configuration, environment);

        AizenConfiguration.Configuration = configuration;

        services.AddHttpContextAccessor();
        if (this.AppInfo.TypeInclude.Contains(AppType.Api))
        {
            services.AddAizenApi(configuration);
            services.AddAizenKeycloakAuth(configuration);
        }

        // Ortak servisler
        services.AddAizenIOC(configuration);
        services.AddAizenCQRS(configuration);
        services.AddAizenMessagebus(configuration, settings =>
        {
            settings.AddConsumer = AppInfo.TypeInclude.Contains(AppType.Worker);
            settings.AddRequestClient = true;
        });
        services.AddAizenRemoteCall(configuration);
        services.AddAizenInfoAccessor(configuration);
        services.AddAizenValidation(configuration);


 

        // Scheduler spesifik servisler
        if (this.AppInfo.TypeInclude.Contains(AppType.Scheduler))
        {
            services.AddAizenRecurringJob();
            services.AddAizenBackgroundJob();
        }

        // Worker spesifik servisler
        if (this.AppInfo.TypeInclude.Contains(AppType.Worker))
        {
            services.AddRazorPages().AddRazorRuntimeCompilation();
            services.AddMvc().AddApplicationPart(GetType().Assembly);
        }

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter your Keycloak access token. Example: Bearer {access_token}"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });
    }
}
