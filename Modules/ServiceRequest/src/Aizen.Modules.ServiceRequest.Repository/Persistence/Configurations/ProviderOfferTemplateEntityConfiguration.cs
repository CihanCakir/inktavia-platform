using Aizen.Modules.ServiceRequest.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.ServiceRequest.Repository.Persistence.Configurations;

public sealed class ProviderOfferTemplateEntityConfiguration : IEntityTypeConfiguration<ProviderOfferTemplateEntity>
{
    public void Configure(EntityTypeBuilder<ProviderOfferTemplateEntity> builder)
    {
        builder.ToTable("provider_offer_templates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.HasIndex(x => x.ProviderProfileId);
        builder.HasMany(x => x.Items).WithOne(x => x.Template).HasForeignKey(x => x.ProviderOfferTemplateId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ProviderOfferTemplateItemEntityConfiguration : IEntityTypeConfiguration<ProviderOfferTemplateItemEntity>
{
    public void Configure(EntityTypeBuilder<ProviderOfferTemplateItemEntity> builder)
    {
        builder.ToTable("provider_offer_template_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Quantity).HasPrecision(12, 3);
        builder.Property(x => x.UnitPrice).HasPrecision(18, 4);
        builder.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(10);
        builder.Property(x => x.UnitCode).HasMaxLength(50);
        builder.Property(x => x.TaxRate).HasPrecision(9, 4);
        builder.Property(x => x.DiscountValue).HasPrecision(18, 2);
        builder.HasIndex(x => x.ProviderOfferTemplateId);
    }
}
