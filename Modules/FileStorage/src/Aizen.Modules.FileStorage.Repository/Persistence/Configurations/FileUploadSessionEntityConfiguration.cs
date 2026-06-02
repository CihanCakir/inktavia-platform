using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Domain.Entities.UploadSession;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.FileStorage.Repository.Persistence.Configurations;

public sealed class FileUploadSessionEntityConfiguration : IEntityTypeConfiguration<FileUploadSessionEntity>
{
    public void Configure(EntityTypeBuilder<FileUploadSessionEntity> builder)
    {
        builder.ToTable("file_upload_sessions", "file_storage");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UploadSessionCode).HasMaxLength(100).IsRequired();
        builder.Property(x => x.BucketName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ObjectKey).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.RequestedFileName).HasMaxLength(500).IsRequired();
        builder.Property(x => x.RequestedContentType).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ClientId).HasMaxLength(200);
        builder.Property(x => x.DeviceId).HasMaxLength(200);

        builder.Property(x => x.Status).HasConversion<int>().IsRequired();

        builder.HasIndex(x => x.UploadSessionCode).IsUnique();
        builder.HasIndex(x => x.FileId);
    }
}
