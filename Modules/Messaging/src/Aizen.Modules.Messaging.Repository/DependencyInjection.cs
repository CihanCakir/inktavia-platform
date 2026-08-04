using Aizen.Modules.Messaging.Domain.Interface;
using Aizen.Modules.Messaging.Domain.Interface.Repository;
using Aizen.Modules.Messaging.Repository.Persistence;
using Aizen.Modules.Messaging.Repository.Repositories;
using Aizen.Modules.Messaging.Repository.Seed;
using Aizen.Modules.Messaging.Repository.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
        services.AddScoped<ServiceRequestChatBackfiller>();
        services.AddScoped<MessagingUserNameResolver>();
        services.AddScoped<ServiceRequestChatNameFixer>();

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

            // SPIKE P1: one-time, idempotent backfill of ServiceRequest chat into the canonical Messaging store.
            // Safe to run every dev boot (dedupes per message). Phase 2 replaces this dev-gate with an explicit flag.
            // Guarded: a backfill failure must never take down messaging-api startup.
            try
            {
                var backfiller = scope.ServiceProvider.GetRequiredService<ServiceRequestChatBackfiller>();
                await backfiller.BackfillAsync(ct);

                // Phase-2 Part C: replace the backfill's role-placeholder names with real names (idempotent).
                var nameFixer = scope.ServiceProvider.GetRequiredService<ServiceRequestChatNameFixer>();
                await nameFixer.FixAsync(ct);
            }
            catch (Exception ex)
            {
                var log = scope.ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger("MessagingBackfill");
                log?.LogError(ex, "[SR→Messaging backfill] aborted (non-fatal); messaging-api continues");
            }
        }
    }
}
