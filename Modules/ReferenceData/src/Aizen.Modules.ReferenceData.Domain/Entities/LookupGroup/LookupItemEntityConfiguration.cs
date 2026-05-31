using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.ReferenceData.Domain.Entities.LookupGroup;

public sealed class LookupItemEntityConfiguration : IEntityTypeConfiguration<LookupItemEntity>
{
    public void Configure(EntityTypeBuilder<LookupItemEntity> builder)
    {
        builder.ToTable("lookup_items", "ref");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LookupGroupId).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(250).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.IconKey).HasMaxLength(150);
        builder.Property(x => x.ColorCode).HasMaxLength(25);
        builder.Property(x => x.SortOrder).IsRequired();
        builder.Property(x => x.IsDefault).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.HasIndex(x => new { x.LookupGroupId, x.Code }).IsUnique();
        builder.HasIndex(x => x.Code);
        builder.HasIndex(x => x.SortOrder);
    }
}
