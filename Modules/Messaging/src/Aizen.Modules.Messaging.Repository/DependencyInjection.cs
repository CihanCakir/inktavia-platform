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

            // BE_WC4b — the one-time SR→Messaging chat backfiller was retired (its job is done; git history preserves it;
            // the SR→Messaging sync consumer is gone — Messaging is now the sole chat store). The idempotent name-fix pass
            // stays to keep any placeholder participant names real. Guarded: a failure must never fail startup.
            try
            {
                var nameFixer = scope.ServiceProvider.GetRequiredService<ServiceRequestChatNameFixer>();
                await nameFixer.FixAsync(ct);
            }
            catch (Exception ex)
            {
                var log = scope.ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger("MessagingNameFix");
                log?.LogError(ex, "[Messaging name-fix] aborted (non-fatal); messaging-api continues");
            }
        }
    }
}
