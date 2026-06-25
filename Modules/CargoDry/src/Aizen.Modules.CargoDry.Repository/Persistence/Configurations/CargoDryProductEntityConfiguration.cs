using Aizen.Modules.CargoDry.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.CargoDry.Repository.Persistence.Configurations;

public sealed class CargoDryProductEntityConfiguration : IEntityTypeConfiguration<CargoDryProductEntity>
{
    public void Configure(EntityTypeBuilder<CargoDryProductEntity> builder)
    {
        builder.ToTable("products");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProductCode).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.ProductCode).IsUnique();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.ValidityDays).IsRequired();
        builder.Property(x => x.HasSmartDevice).IsRequired();
        builder.Property(x => x.RetailPrice).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
    }
}
