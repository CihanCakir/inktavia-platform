using Aizen.Modules.CargoDry.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.CargoDry.Repository.Persistence.Configurations;

public sealed class CargoDryProductImageEntityConfiguration : IEntityTypeConfiguration<CargoDryProductImageEntity>
{
    public void Configure(EntityTypeBuilder<CargoDryProductImageEntity> builder)
    {
        builder.ToTable("product_images");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProductId).IsRequired();
        builder.Property(x => x.FileId).IsRequired();
        builder.Property(x => x.SortOrder).IsRequired();

        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => new { x.ProductId, x.FileId }).IsUnique();

        // IsActive / CreateDate / ModifyDate: mapped by AizenEntityWithAudit base configuration
    }
}
