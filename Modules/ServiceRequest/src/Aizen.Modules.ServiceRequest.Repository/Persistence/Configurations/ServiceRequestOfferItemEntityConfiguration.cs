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
        // BE-S3: raw foreign price before submit-time conversion; nullable → existing rows = TRY-native (back-compat).
        builder.Property(x => x.SourceUnitPrice).HasPrecision(18, 4);
        builder.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(10);
        builder.Property(x => x.UnitCode).HasMaxLength(50);
        builder.Property(x => x.TaxRate).HasPrecision(9, 4);
        builder.Property(x => x.DiscountValue).HasPrecision(18, 2);
        builder.Property(x => x.LineSubtotal).HasPrecision(18, 2);
        builder.Property(x => x.TaxAmount).HasPrecision(18, 2);
        builder.Property(x => x.LineTotal).HasPrecision(18, 2);
        builder.Property(x => x.DiscountAmount).HasPrecision(18, 2);

        // ── Line economics (BE-S1) ─────────────────────────────────────────────
        builder.Property(x => x.PricingMethod).HasConversion<int>().IsRequired().HasDefaultValue(Aizen.Modules.ServiceRequest.Abstraction.Enum.PricingMethod.Fixed);
        builder.Property(x => x.CommissionEligibility).HasConversion<int>().IsRequired().HasDefaultValue(Aizen.Modules.ServiceRequest.Abstraction.Enum.LineCommissionEligibility.InheritFromCategory);
        builder.Property(x => x.CommissionBaseAmount).HasPrecision(18, 4).IsRequired().HasDefaultValue(0m);

        // ── Customer discount funding (BE-S6) ──
        builder.Property(x => x.LineDiscountEligibility).HasConversion<int>().IsRequired().HasDefaultValue(Aizen.Modules.ServiceRequest.Abstraction.Enum.LineDiscountEligibility.InheritFromCategory);
        builder.Property(x => x.CustomerDiscountAmount).HasPrecision(18, 2).IsRequired().HasDefaultValue(0m);
        builder.Property(x => x.PlatformFundedDiscountAmount).HasPrecision(18, 2).IsRequired().HasDefaultValue(0m);
        builder.Property(x => x.ProviderFundedDiscountAmount).HasPrecision(18, 2).IsRequired().HasDefaultValue(0m);

        builder.HasIndex(x => x.ServiceRequestOfferId);
    }
}
