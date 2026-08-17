using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Domain.Entities.File;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.FileStorage.Repository.Persistence.Configurations;

public sealed class FileEntityConfiguration : IEntityTypeConfiguration<FileEntity>
{
    public void Configure(EntityTypeBuilder<FileEntity> builder)
    {
        builder.ToTable("files", "file_storage");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileCode).HasMaxLength(100).IsRequired();
        builder.Property(x => x.OriginalFileName).HasMaxLength(500).IsRequired();
        builder.Property(x => x.StoredFileName).HasMaxLength(500).IsRequired();
        builder.Property(x => x.BucketName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ObjectKey).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Extension).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Checksum).HasMaxLength(256);

        builder.Property(x => x.StorageProvider).HasConversion<int>().IsRequired();
        builder.Property(x => x.Visibility).HasConversion<int>().IsRequired();
        builder.Property(x => x.Category).HasConversion<int>().IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();

        // Override the base class [DatabaseGenerated(Computed)] — FileStorage assigns PublicId
        // application-side in FileEntity.Create. Without this, EF skips the value on INSERT.
        builder.Property(x => x.PublicId)
               .ValueGeneratedNever()
               .IsRequired();

        builder.HasIndex(x => x.FileCode).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.Category);

        builder.HasMany(f => f.OwnerReferences)
            .WithOne(x => x.File)
            .HasForeignKey(x => x.FileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(f => f.UploadSessions)
            .WithOne(x => x.File)
            .HasForeignKey(x => x.FileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(f => f.ProcessingJobs)
            .WithOne(x => x.File)
            .HasForeignKey(x => x.FileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(f => f.OwnerReferences).HasField("_ownerReferences");
        builder.Navigation(f => f.UploadSessions).HasField("_uploadSessions");
        builder.Navigation(f => f.ProcessingJobs).HasField("_processingJobs");
    }
}
