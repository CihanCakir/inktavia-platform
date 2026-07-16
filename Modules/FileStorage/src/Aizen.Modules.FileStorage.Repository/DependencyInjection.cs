using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Domain.Entities.File;
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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.FileStorage.Repository;

public static class DependencyInjection
{
    public static IServiceCollection AddFileStorageRepository(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<S3ObjectStorageOptions>(configuration.GetSection("S3ObjectStorage"));
        services.AddSingleton<IValidateOptions<S3ObjectStorageOptions>, S3ObjectStorageOptionsValidator>();
        services.AddSingleton<IObjectStorageProvider, S3ObjectStorageProvider>();

        services.AddScoped<IFileRepository, FileRepository>();
        services.AddScoped<IFileOwnerReferenceRepository, FileOwnerReferenceRepository>();
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
        services.AddSingleton<IFileScanner, NoOpFileScanner>();

        return services;
    }

    public static async Task SeedFileStorageAsync(this IHost host, CancellationToken ct = default)
    {
        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FileStorageDbContext>();
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(ct);
        if (pendingMigrations.Any())
            await dbContext.Database.MigrateAsync(ct);

        await SeedServiceRequestAttachmentFileAsync(scope.ServiceProvider, dbContext, ct);
    }

    /// <summary>
    /// Seeds one real image file in MinIO + File row for the SR 9011 emergency request attachment.
    /// PublicId MUST equal ServiceRequest SeedAttachmentsAsync's fileId (a0a0a0a0-b1b1-c2c2-d3d3-e4e4e4e4e4e4).
    /// </summary>
    private static async Task SeedServiceRequestAttachmentFileAsync(
        IServiceProvider sp, FileStorageDbContext db, CancellationToken ct)
    {
        var logger = sp.GetRequiredService<ILogger<FileStorageDbContext>>();
        var publicId = new Guid("a0a0a0a0-b1b1-c2c2-d3d3-e4e4e4e4e4e4");

        // Dev/Local only — never seed demo files into a real environment.
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        if (!env.Equals("Development", StringComparison.OrdinalIgnoreCase) &&
            !env.Equals("Local", StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            var storageProvider = sp.GetRequiredService<IObjectStorageProvider>();
            var options = sp.GetRequiredService<IOptions<S3ObjectStorageOptions>>().Value;

            var bucket = options.BucketName;
            const string objectKey = "image/seed/service-requests/9011/dumen-hasar.png";

            // A real 480×320 PNG placeholder (framed "SEED PHOTO / SR 9011 dumen hasar" card) so the
            // gallery lightbox shows an actual image, not a 1px dot. Swap a nicer asset later if desired.
            var bytes = Convert.FromBase64String(SeedPhotoPngBase64);

            // Always (re)write the object — cheap, idempotent by key. This refreshes the bytes even when the
            // DB row already exists (e.g. an earlier seed wrote a placeholder), so a restart is enough to fix it.
            await storageProvider.PutObjectAsync(bucket, objectKey, bytes, "image/png", ct);

            // The File row is inserted once (guarded by PublicId). PublicId MUST equal the ServiceRequest
            // attachment seed's fileId; raw SQL sets it explicitly and the WHERE NOT EXISTS keeps it idempotent.
            await db.Database.ExecuteSqlRawAsync(@"
                INSERT INTO file_storage.files
                (""FileCode"", ""OriginalFileName"", ""StoredFileName"", ""BucketName"", ""ObjectKey"",
                 ""ContentType"", ""Extension"", ""SizeInBytes"", ""StorageProvider"", ""Visibility"",
                 ""Category"", ""Status"", ""PublicId"", ""UploadedAt"", ""IsActive"", ""IsDeleted"",
                 ""CreateDate"", ""ModifyDate"")
                SELECT 'SEEDSR9011PHOTO', 'dumen-hasar.png', 'dumen-hasar.png',
                       @p0, @p1, 'image/png', 'png', @p2, 2, 2, 1, 5,
                       @p3, NOW(), true, false, NOW(), NOW()
                WHERE NOT EXISTS (SELECT 1 FROM file_storage.files WHERE ""PublicId"" = @p3)",
                bucket, objectKey, (long)bytes.Length, publicId);

            logger.LogInformation("Seeded SR attachment file: PublicId={PublicId}, ObjectKey={ObjectKey}", publicId, objectKey);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to seed SR attachment file (non-fatal).");
        }
    }

    // 480×320 PNG placeholder for the SR 9011 attachment (dev/local seed only).
    private const string SeedPhotoPngBase64 =
        "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";
}
