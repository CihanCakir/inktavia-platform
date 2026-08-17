using Aizen.Modules.ServiceRequest.Domain.Entities.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.ServiceRequest.Repository.Persistence.Configurations;

/// <summary>
/// BE-S4a — per-offer-line travel-pricing detail. At most one detail per offer line. Distance precision mirrors the offer
/// line's Quantity (12,3); per-km rate mirrors UnitPrice (18,4). Descriptive metadata, not part of the line money math.
/// </summary>
public sealed class TravelPricingDetailConfiguration : IEntityTypeConfiguration<TravelPricingDetailEntity>
{
    public void Configure(EntityTypeBuilder<TravelPricingDetailEntity> b)
    {
        b.ToTable("travel_pricing_details");
        b.HasKey(x => x.Id);

        b.Property(x => x.OfferItemId).IsRequired();
        b.Property(x => x.Method).HasConversion<int>().IsRequired();
        b.Property(x => x.OriginCityCode).HasMaxLength(50);
        b.Property(x => x.DestinationCityCode).HasMaxLength(50);
        b.Property(x => x.DistanceKm).HasPrecision(12, 3);
        b.Property(x => x.PerKmRate).HasPrecision(18, 4);
        b.Property(x => x.UnitCode).HasMaxLength(50);

        b.HasIndex(x => x.OfferItemId).IsUnique();
    }
}
