using Aizen.Modules.ServiceRequest.Domain.Entities.Offer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.ServiceRequest.Repository.Persistence.Configurations;

public sealed class ServiceRequestOfferItemEntityConfiguration : IEntityTypeConfiguration<ServiceRequestOfferItemEntity>
{
    public void Configure(EntityTypeBuilder<ServiceRequestOfferItemEntity> builder)
    {
        builder.ToTable("service_request_offer_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Quantity).HasPrecision(12, 3);
        builder.Property(x => x.UnitPrice).IsRequired().HasPrecision(18, 4);
        builder.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(10);
        builder.Property(x => x.UnitCode).HasMaxLength(50);
        builder.Property(x => x.TaxRate).HasPrecision(9, 4);
        builder.Property(x => x.DiscountValue).HasPrecision(18, 2);
        builder.Property(x => x.LineSubtotal).HasPrecision(18, 2);
        builder.Property(x => x.TaxAmount).HasPrecision(18, 2);
        builder.Property(x => x.LineTotal).HasPrecision(18, 2);
        builder.Property(x => x.DiscountAmount).HasPrecision(18, 2);
        builder.HasIndex(x => x.ServiceRequestOfferId);
    }
}
