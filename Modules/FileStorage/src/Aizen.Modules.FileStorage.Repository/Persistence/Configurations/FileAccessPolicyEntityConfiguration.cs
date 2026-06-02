using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Domain.Entities.Access;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.FileStorage.Repository.Persistence.Configurations;

public sealed class FileAccessPolicyEntityConfiguration : IEntityTypeConfiguration<FileAccessPolicyEntity>
{
    public void Configure(EntityTypeBuilder<FileAccessPolicyEntity> builder)
    {
        builder.ToTable("file_access_policies", "file_storage");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AllowedOwnerModule).HasMaxLength(100);
        builder.Property(x => x.AllowedOwnerEntityType).HasMaxLength(200);
        builder.Property(x => x.AllowedOperations).HasMaxLength(500).IsRequired();

        builder.Property(x => x.Visibility).HasConversion<int>().IsRequired();

        builder.HasIndex(x => x.FileId).IsUnique();

        builder.HasOne(x => x.File)
            .WithMany()
            .HasForeignKey(x => x.FileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
