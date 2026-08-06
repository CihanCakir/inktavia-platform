using Aizen.Modules.ServiceRequest.Domain.Entities.ChangeOrder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.ServiceRequest.Repository.Persistence.Configurations;

public sealed class ServiceChangeOrderItemEntityConfiguration : IEntityTypeConfiguration<ServiceChangeOrderItemEntity>
{
    public void Configure(EntityTypeBuilder<ServiceChangeOrderItemEntity> builder)
    {
        builder.ToTable("service_change_order_items");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ItemType).IsRequired().HasConversion<int>();
        builder.Property(x => x.Title).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Quantity).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.UnitPrice).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(10);
        builder.Property(x => x.UnitCode).HasMaxLength(20);
        builder.Property(x => x.TaxRate).HasPrecision(9, 4).IsRequired().HasDefaultValue(0m);
        builder.Property(x => x.DiscountType).IsRequired().HasConversion<int>();
        builder.Property(x => x.DiscountValue).HasPrecision(18, 4);
        builder.Property(x => x.PricingMethod).IsRequired().HasConversion<int>();
        builder.Property(x => x.CommissionEligibility).IsRequired().HasConversion<int>();
        builder.Property(x => x.LineDiscountEligibility).IsRequired().HasConversion<int>();

        builder.HasIndex(x => x.ServiceChangeOrderId);
    }
}
