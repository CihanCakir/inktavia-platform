using Aizen.Modules.ServiceRequest.Domain.Entities.Offer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.ServiceRequest.Repository.Persistence.Configurations;

/// <summary>
/// BE-S3b — EF mapping for the offer-level FX rate snapshot. One row per non-TRY source currency; timestamps are UTC
/// (timestamptz). FK → service_request_offers (OnDelete Cascade — a re-submit replaces the rows; they are logically frozen
/// once the offer is accepted because no code path returns an accepted offer to Draft).
/// </summary>
public sealed class OfferFxSnapshotEntityConfiguration : IEntityTypeConfiguration<OfferFxSnapshotEntity>
{
    public void Configure(EntityTypeBuilder<OfferFxSnapshotEntity> builder)
    {
        builder.ToTable("service_request_offer_fx_snapshots");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ServiceRequestOfferId).IsRequired();
        builder.Property(x => x.SourceCurrencyCode).IsRequired().HasMaxLength(10);
        builder.Property(x => x.SettlementCurrencyCode).IsRequired().HasMaxLength(10);
        builder.Property(x => x.Rate).IsRequired().HasPrecision(18, 6);
        builder.Property(x => x.RateDate).IsRequired();
        builder.Property(x => x.ResolvedAtUtc).IsRequired();

        builder.HasIndex(x => x.ServiceRequestOfferId);
        builder.HasIndex(x => new { x.ServiceRequestOfferId, x.SourceCurrencyCode }).IsUnique();
    }
}
