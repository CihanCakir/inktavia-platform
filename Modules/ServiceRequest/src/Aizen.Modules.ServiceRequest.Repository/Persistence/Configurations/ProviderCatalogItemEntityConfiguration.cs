using Aizen.Modules.ServiceRequest.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.ServiceRequest.Repository.Persistence.Configurations;

public sealed class ProviderCatalogItemEntityConfiguration : IEntityTypeConfiguration<ProviderCatalogItemEntity>
{
    public void Configure(EntityTypeBuilder<ProviderCatalogItemEntity> builder)
    {
        builder.ToTable("provider_catalog_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.DefaultQuantity).HasPrecision(12, 3);
        builder.Property(x => x.DefaultUnitPrice).HasPrecision(18, 4);
        builder.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(10);
        builder.Property(x => x.UnitCode).HasMaxLength(50);
        builder.Property(x => x.DefaultTaxRate).HasPrecision(9, 4);
        builder.HasIndex(x => x.ProviderProfileId);
    }
}
