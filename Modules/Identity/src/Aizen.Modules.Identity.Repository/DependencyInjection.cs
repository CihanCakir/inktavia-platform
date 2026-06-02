using Aizen.Modules.Identity.Abstraction.Model;
using Aizen.Modules.Identity.Domain.Interface;
using Aizen.Modules.Identity.Domain.Interface.Repository;
using Aizen.Modules.Identity.Domain.Interface.Service;
using Aizen.Modules.Identity.Repository.Context.Seed;
using Aizen.Modules.Identity.Repository.Identity;
using Aizen.Modules.Identity.Repository.Identity.Repository;
using Aizen.Modules.Identity.Repository.Identity.Service;
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
            services.AddScoped<IUserRepository, UserRepository>();

            return services;
        }

        public static IServiceCollection AddInktaviaService(this IServiceCollection services, IConfiguration? configuration = null)
        {
            // OAuth Provider Client + HttpClient
            if (configuration is not null)
            {
                services.Configure<OAuthOptions>(configuration.GetSection("OAuth")); // appsettings: OAuth:Google/Apple ...
            }

            services.AddScoped<IAuthorizationService, AuthorizationService>();
            services.AddScoped<IConsumerRegistrationDomainService, ConsumerRegistrationDomainService>();
            services.AddScoped<IInktaviaTokenService, InktaviaTokenService>();
            services.AddScoped<IOAuthProviderClient, OAuthProviderClient>();
            services.AddScoped<IOrganizerRegistrationDomainService, OrganizerRegistrationDomainService>();
            services.AddScoped<IVenueRegistrationDomainService, VenueRegistrationDomainService>();



            return services;
        }


        public static async Task SeedIdentityAsync(this IHost host, CancellationToken ct = default)
        {
            using var scope = host.Services.CreateScope();
            await SeedIdentityBase.RunAsync(scope.ServiceProvider, ct);
        }
    }

}