using Aizen.Core.EFCore;
using Aizen.Modules.FileStorage.Domain.Entities.Access;
using Aizen.Modules.FileStorage.Domain.Entities.File;
using Aizen.Modules.FileStorage.Domain.Entities.Processing;
using Aizen.Modules.FileStorage.Domain.Entities.UploadSession;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.FileStorage.Repository.Persistence;

[DocumentationInfo("FileStorage EF DbContext", "EF Core context for the file_storage PostgreSQL schema.")]
public sealed class FileStorageDbContext : AizenDbContext
{
    public FileStorageDbContext(DbContextOptions<FileStorageDbContext> options) : base(options) { }

    public DbSet<FileEntity> Files => Set<FileEntity>();
    public DbSet<FileOwnerReferenceEntity> FileOwnerReferences => Set<FileOwnerReferenceEntity>();
    public DbSet<FileUploadSessionEntity> FileUploadSessions => Set<FileUploadSessionEntity>();
    public DbSet<FileProcessingJobEntity> FileProcessingJobs => Set<FileProcessingJobEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("file_storage");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FileStorageDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        NormalizeDateTimeProperties();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        NormalizeDateTimeProperties();
        return base.SaveChanges();
    }

    private void NormalizeDateTimeProperties()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
                continue;
            foreach (var property in entry.Properties)
            {
                if (property.CurrentValue is DateTime dt && dt.Kind == DateTimeKind.Local)
                    property.CurrentValue = dt.ToUniversalTime();
            }
        }
    }
}
