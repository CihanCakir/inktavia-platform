using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Persistence;
using Aizen.Modules.ServiceRequest.Repository.Repositories;
using Aizen.Modules.ServiceRequest.Repository.Seed.MockData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aizen.Modules.ServiceRequest.Repository;

public static class DependencyInjection
{
    public static IServiceCollection AddServiceRequestRepository(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IServiceRequestRepository, ServiceRequestRepository>();
        services.AddScoped<IServiceRequestOfferRepository, ServiceRequestOfferRepository>();
        services.AddScoped<IServiceRequestAssignmentRepository, ServiceRequestAssignmentRepository>();
        services.AddScoped<IServiceRequestMessageRepository, ServiceRequestMessageRepository>();
        services.AddScoped<IServiceRequestWorkLogRepository, ServiceRequestWorkLogRepository>();
        services.AddScoped<IServiceRequestCompletionRepository, ServiceRequestCompletionRepository>();
        services.AddScoped<IServiceRequestDisputeRepository, ServiceRequestDisputeRepository>();

        return services;
    }

    public static IServiceCollection AddServiceRequestServices(this IServiceCollection services)
    {
        return services;
    }

    public static IServiceCollection AddServiceRequestMockData(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MockDataSeedOptions>(configuration.GetSection("MockData"));
        services.AddScoped<ServiceRequestMockDataSeeder>(sp =>
        {
            var db = sp.GetRequiredService<ServiceRequestDbContext>();
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MockDataSeedOptions>>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ServiceRequestMockDataSeeder>>();
            var env = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            return new ServiceRequestMockDataSeeder(db, options, logger, env);
        });
        return services;
    }

    public static async Task SeedServiceRequestAsync(this IHost host, CancellationToken ct = default)
    {
        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ServiceRequestDbContext>();
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(ct);
        if (pendingMigrations.Any())
            await dbContext.Database.MigrateAsync(ct);

        // Run mock data seeder
        var mockSeeder = scope.ServiceProvider.GetRequiredService<ServiceRequestMockDataSeeder>();
        await mockSeeder.SeedAsync(ct);
    }
}
