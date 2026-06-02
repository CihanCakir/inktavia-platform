using Aizen.Modules.FileStorage.Domain.Entities.Access;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.FileStorage.Repository.Persistence.Configurations;

public sealed class FileOwnerReferenceEntityConfiguration : IEntityTypeConfiguration<FileOwnerReferenceEntity>
{
    public void Configure(EntityTypeBuilder<FileOwnerReferenceEntity> builder)
    {
        builder.ToTable("file_owner_references", "file_storage");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OwnerModule).HasMaxLength(100).IsRequired();
        builder.Property(x => x.OwnerEntityType).HasMaxLength(200).IsRequired();

        builder.HasIndex(x => new { x.FileId, x.OwnerModule, x.OwnerEntityType, x.OwnerEntityId });
        builder.HasIndex(x => new { x.OwnerModule, x.OwnerEntityType, x.OwnerEntityId });
    }
}
