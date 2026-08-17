using Aizen.Modules.Messaging.Domain.Interface;
using Aizen.Modules.Messaging.Domain.Interface.Repository;
using Aizen.Modules.Messaging.Repository.Persistence;
using Aizen.Modules.Messaging.Repository.Repositories;
using Aizen.Modules.Messaging.Repository.Seed;
using Aizen.Modules.Messaging.Repository.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aizen.Modules.Messaging.Repository;

[DocumentationInfo("Messaging DI registration",
    "Registers all messaging module services, repositories, and seed data.")]
public static class DependencyInjection
{
    public static IServiceCollection AddMessagingRepository(this IServiceCollection services)
    {
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IConversationMessageRepository, ConversationMessageRepository>();
        services.AddScoped<IMessageContentPolicy, MessageContentPolicyService>();
        services.AddScoped<MessagingMockDataSeeder>();

        return services;
    }

    public static IServiceCollection AddMessagingServices(this IServiceCollection services)
    {
        return services;
    }

    public static async Task SeedMessagingAsync(this IHost host, CancellationToken ct = default)
    {
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MessagingDbContext>();

        var pending = await db.Database.GetPendingMigrationsAsync(ct);
        if (pending.Any())
            await db.Database.MigrateAsync(ct);

        var env = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
        if (env.IsDevelopment())
        {
            var seeder = scope.ServiceProvider.GetRequiredService<MessagingMockDataSeeder>();
            await seeder.SeedAsync(ct);
        }
    }
}
