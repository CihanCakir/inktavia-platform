using Aizen.Modules.Identity.Domain.Interface;
using Aizen.Modules.Identity.Domain.Interface.Repository;
using Aizen.Modules.Identity.Domain.Interface.Service;
using Aizen.Modules.Identity.Repository.Context.Seed;
using Aizen.Modules.Identity.Repository.Context.Seed.MockData;
using Aizen.Modules.Identity.Repository.Identity;
using Aizen.Modules.Identity.Repository.Identity.Repository;
using Aizen.Modules.Identity.Repository.Identity.Service;
using Aizen.Modules.Identity.Repository.Identity.Service.Onboarding;
using Aizen.Modules.Identity.Repository.Identity.Service.OtpLogin;
using Aizen.Modules.Identity.Repository.Identity.Service.PasswordRecovery;
using Aizen.Modules.Identity.Repository.Identity.Service.EmailVerification;
using Aizen.Modules.Identity.Repository.Seed.MockData;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aizen.Modules.Identity.Repository
{
    public static class DependencyInjection
    {


        public static IServiceCollection AddInktaviaRepository(this IServiceCollection services)
        {
            services.AddScoped<IAgreementRepository, AgreementRepository>();
            services.AddScoped<IUserDeviceRepository, UserDeviceRepository>();
            services.AddScoped<IUserLoginTokenRepository, UserLoginTokenRepository>();
            services.AddScoped<IUserMessagePermissionRepository, UserMessagePermissionRepository>();
            services.AddScoped<IUserProfileRepository, UserProfileRepository>();
            services.AddScoped<IProviderServiceCategoryRepository, ProviderServiceCategoryRepository>();
            services.AddScoped<Context.Seed.ProviderEligibilityBackfillSeeder>();
            services.AddScoped<IUserRepository, UserRepository>();

            return services;
        }

        public static IServiceCollection AddInktaviaService(this IServiceCollection services, IConfiguration? configuration = null)
        {
            // OAuth Provider Client + HttpClient
            if (configuration is not null)
            {
                services.Configure<OAuthOptions>(configuration.GetSection("OAuth")); // appsettings: OAuth:Google/Apple ...
                services.Configure<Abstraction.Options.IdentityKeycloakOptions>(
                    configuration.GetSection(Abstraction.Options.IdentityKeycloakOptions.SectionName));
            }

            services.AddScoped<IProviderKeycloakRoleSyncService, ProviderKeycloakRoleSyncService>();
            services.AddScoped<IAuthorizationService, AuthorizationService>();
            services.AddScoped<IConsumerRegistrationDomainService, ConsumerRegistrationDomainService>();
            services.AddScoped<IInktaviaTokenService, InktaviaTokenService>();
            services.AddScoped<IOAuthProviderClient, OAuthProviderClient>();
            services.AddScoped<IOrganizerRegistrationDomainService, OrganizerRegistrationDomainService>();
            services.AddScoped<IOrganizerKeycloakProvisioningDomainService, OrganizerKeycloakProvisioningDomainService>();
            services.AddScoped<IParticipantKeycloakProvisioningDomainService, ParticipantKeycloakProvisioningDomainService>();
            services.AddScoped<IAdminKeycloakProvisioningDomainService, AdminKeycloakProvisioningDomainService>();
            services.AddScoped<IVenueRegistrationDomainService, VenueRegistrationDomainService>();

            // Provider onboarding
            services.AddScoped<IProviderOnboardingRepository, ProviderOnboardingRepository>();
            services.AddScoped<IProviderOnboardingDomainService, ProviderOnboardingDomainService>();
            services.AddScoped<IFileAttachmentValidationService, FileAttachmentValidationService>();

            // Password recovery (Identity-owned)
            if (configuration is not null)
            {
                services.Configure<PasswordRecoveryOptions>(
                    configuration.GetSection(PasswordRecoveryOptions.SectionName));
            }
            services.AddScoped<IIdentityKeycloakPasswordService, IdentityKeycloakPasswordService>();
            services.AddScoped<IProviderPasswordRecoveryDomainService, ProviderPasswordRecoveryDomainService>();
            // Participant (mobile) recovery mirrors the provider vertical — reuses the shared options,
            // Keycloak password service and notifier registered here; only the domain service differs.
            services.AddScoped<IParticipantPasswordRecoveryDomainService, ParticipantPasswordRecoveryDomainService>();

            var deliveryMode = configuration?.GetValue<string>("PasswordRecovery:DeliveryMode") ?? "Notification";
            if (deliveryMode.Equals("Logging", StringComparison.OrdinalIgnoreCase))
                services.AddSingleton<IProviderPasswordRecoveryNotifier, LoggingProviderPasswordRecoveryNotifier>();
            else
                services.AddScoped<IProviderPasswordRecoveryNotifier, MessageBusProviderPasswordRecoveryNotifier>();

            // OTP login (Identity-owned, separate from recovery)
            if (configuration is not null)
            {
                services.Configure<OtpLoginOptions>(
                    configuration.GetSection(OtpLoginOptions.SectionName));
            }
            services.AddScoped<IProviderOtpLoginDomainService, ProviderOtpLoginDomainService>();
            services.AddScoped<IAdminOtpLoginDomainService, AdminOtpLoginDomainService>();
            services.AddScoped<IParticipantOtpLoginDomainService, ParticipantOtpLoginDomainService>();

            // OTP login ticket (single-use HMAC-SHA256 signed tickets)
            if (configuration is not null)
            {
                services.Configure<OtpLoginTicketOptions>(
                    configuration.GetSection(OtpLoginTicketOptions.SectionName));
            }
            services.AddScoped<IProviderOtpLoginTicketService, ProviderOtpLoginTicketService>();

            if (deliveryMode.Equals("Logging", StringComparison.OrdinalIgnoreCase))
                services.AddSingleton<IProviderOtpLoginNotifier, LoggingProviderOtpLoginNotifier>();
            else
                services.AddScoped<IProviderOtpLoginNotifier, MessageBusProviderOtpLoginNotifier>();

            // E-posta doğrulama (Identity sahipliğinde, uygulama token'ı — Keycloak execute-actions-email yerine)
            if (configuration is not null)
            {
                services.Configure<ProviderEmailVerificationOptions>(
                    configuration.GetSection(ProviderEmailVerificationOptions.SectionName));
            }
            services.AddScoped<IProviderEmailVerificationDomainService, ProviderEmailVerificationDomainService>();

            var emailVerifyDeliveryMode =
                configuration?.GetValue<string>($"{ProviderEmailVerificationOptions.SectionName}:DeliveryMode") ?? "Notification";
            if (emailVerifyDeliveryMode.Equals("Logging", StringComparison.OrdinalIgnoreCase))
                services.AddSingleton<IProviderEmailVerificationNotifier, LoggingProviderEmailVerificationNotifier>();
            else
                services.AddScoped<IProviderEmailVerificationNotifier, MessageBusProviderEmailVerificationNotifier>();

            return services;
        }


        public static IServiceCollection AddIdentityMockData(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<MockDataSeedOptions>(configuration.GetSection("MockData"));
            services.AddScoped<IdentityMockDataSeeder>(sp =>
            {
                var db = sp.GetRequiredService<Context.IdentityDbContext>();
                var hasher = sp.GetRequiredService<IPasswordHasher<Domain.Entities.UserEntity>>();
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MockDataSeedOptions>>();
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<IdentityMockDataSeeder>>();
                var env = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
                return new IdentityMockDataSeeder(db, hasher, options, logger, env);
            });
            return services;
        }

        public static async Task SeedIdentityAsync(this IHost host, CancellationToken ct = default)
        {
            using var scope = host.Services.CreateScope();
            await SeedIdentityBase.RunAsync(scope.ServiceProvider, ct);

            // Run mock data seeder
            var mockSeeder = scope.ServiceProvider.GetRequiredService<IdentityMockDataSeeder>();
            await mockSeeder.SeedAsync(ct);

            // I2 — idempotent backfill of provider City + service categories from onboarding drafts.
            var eligibilityBackfill = scope.ServiceProvider
                .GetRequiredService<Context.Seed.ProviderEligibilityBackfillSeeder>();
            await eligibilityBackfill.SeedAsync(ct);
        }
    }

}