using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Persistence;
using Aizen.Modules.ServiceRequest.Repository.Repositories;
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

    public static async Task SeedServiceRequestAsync(this IHost host, CancellationToken ct = default)
    {
        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ServiceRequestDbContext>();
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(ct);
        if (pendingMigrations.Any())
            await dbContext.Database.MigrateAsync(ct);
    }
}
