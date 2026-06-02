using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.ReferenceData.Domain.Entities.LookupGroup;

public sealed class LookupGroupEntityConfiguration : IEntityTypeConfiguration<LookupGroupEntity>
{
    public void Configure(EntityTypeBuilder<LookupGroupEntity> builder)
    {
        builder.ToTable("lookup_groups", "ref");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ParentLookupGroupId).IsRequired(false);
        builder.Property(x => x.Code).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(250).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.GroupType).IsRequired();
        builder.Property(x => x.Level).IsRequired();
        builder.Property(x => x.HierarchyPath).HasMaxLength(500).IsRequired();
        builder.Property(x => x.SortOrder).IsRequired();
        builder.Property(x => x.IsSystemGroup).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();

        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.ParentLookupGroupId);
        builder.HasIndex(x => x.HierarchyPath);
        builder.HasIndex(x => new { x.ParentLookupGroupId, x.Code }).IsUnique();

        builder.HasOne(x => x.ParentLookupGroup)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentLookupGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Items)
            .WithOne(x => x.LookupGroup)
            .HasForeignKey(x => x.LookupGroupId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
