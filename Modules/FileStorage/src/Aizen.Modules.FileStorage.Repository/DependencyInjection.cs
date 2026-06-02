using Aizen.Modules.FileStorage.Domain.Interface.Repository;
using Aizen.Modules.FileStorage.Domain.Interface.Service;
using Aizen.Modules.FileStorage.Repository.Persistence;
using Aizen.Modules.FileStorage.Repository.Providers.S3;
using Aizen.Modules.FileStorage.Repository.Repositories;
using Aizen.Modules.FileStorage.Repository.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aizen.Modules.FileStorage.Repository;

public static class DependencyInjection
{
    public static IServiceCollection AddFileStorageRepository(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<S3ObjectStorageOptions>(configuration.GetSection("S3ObjectStorage"));
        services.AddSingleton<IObjectStorageProvider, S3ObjectStorageProvider>();

        services.AddScoped<IFileRepository, FileRepository>();
        services.AddScoped<IFileVersionRepository, FileVersionRepository>();
        services.AddScoped<IFileOwnerReferenceRepository, FileOwnerReferenceRepository>();
        services.AddScoped<IFileAccessPolicyRepository, FileAccessPolicyRepository>();
        services.AddScoped<IFileUploadSessionRepository, FileUploadSessionRepository>();
        services.AddScoped<IFileProcessingJobRepository, FileProcessingJobRepository>();
        services.AddScoped<IFileMetadataDocumentRepository, FileMetadataDocumentRepository>();

        return services;
    }

    public static IServiceCollection AddFileStorageServices(this IServiceCollection services)
    {
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddScoped<IFileUploadSessionService, FileUploadSessionService>();
        services.AddScoped<IFileAccessService, FileAccessService>();
        services.AddScoped<IFileOwnershipService, FileOwnershipService>();
        services.AddScoped<IFileProcessingService, FileProcessingService>();
        services.AddScoped<IFileValidationService, FileValidationService>();
        services.AddSingleton<IFileCacheKeyService, FileCacheKeyService>();
        services.AddScoped<IFileCacheInvalidationService, FileCacheInvalidationService>();

        return services;
    }

    public static async Task SeedFileStorageAsync(this IHost host, CancellationToken ct = default)
    {
        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FileStorageDbContext>();
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(ct);
        if (pendingMigrations.Any())
            await dbContext.Database.MigrateAsync(ct);
    }
}
