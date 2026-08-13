using Aizen.Modules.Content.Domain.Interface.Repository;
using Aizen.Modules.Content.Repository.Persistence;
using Aizen.Modules.Content.Repository.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aizen.Modules.Content.Repository;

public static class DependencyInjection
{
    public static IServiceCollection AddContentRepository(
        this IServiceCollection services, IConfiguration configuration)
    {
        // ── MongoDB ────────────────────────────────────────────────────────────
        // IMongoClient and IMongoDatabase are NOT registered here.
        // AddAizenMongo() in Program.cs auto-discovers ContentMongoDbContext and
        // the IAizenMongoRepositoryFactory<ContentMongoDbContext> it backs.
        services.AddScoped<IContentItemRepository,     ContentItemRepository>();
        services.AddScoped<IContentCommentRepository,  ContentCommentRepository>();
        services.AddScoped<IContentFavoriteRepository, ContentFavoriteRepository>();
        services.AddScoped<IContentCategoryRepository, ContentCategoryRepository>();
        services.AddScoped<ContentMongoIndexInitializer>();

        return services;
    }

    public static async Task SeedContentAsync(this IHost host, CancellationToken ct = default)
    {
        using var scope = host.Services.CreateScope();

        var mongoIndexer = scope.ServiceProvider.GetRequiredService<ContentMongoIndexInitializer>();
        await mongoIndexer.InitializeAsync(ct);

        // NOTE: Optional demo content seeding (behind a config flag) is added in Phase C9.
    }
}
