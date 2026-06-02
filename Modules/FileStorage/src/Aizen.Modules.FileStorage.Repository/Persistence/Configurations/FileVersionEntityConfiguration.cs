using Aizen.Modules.FileStorage.Domain.Entities.File;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.FileStorage.Repository.Persistence.Configurations;

public sealed class FileVersionEntityConfiguration : IEntityTypeConfiguration<FileVersionEntity>
{
    public void Configure(EntityTypeBuilder<FileVersionEntity> builder)
    {
        builder.ToTable("file_versions", "file_storage");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BucketName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ObjectKey).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Checksum).HasMaxLength(256);

        builder.HasIndex(x => new { x.FileId, x.VersionNo }).IsUnique();

        builder.HasOne(x => x.File)
            .WithMany()
            .HasForeignKey(x => x.FileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
