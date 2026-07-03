using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.CargoDry.Repository.Persistence.Configurations;

public sealed class CargoDryKitEntityConfiguration : IEntityTypeConfiguration<CargoDryKitEntity>
{
    public void Configure(EntityTypeBuilder<CargoDryKitEntity> builder)
    {
        builder.ToTable("kits");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SerialNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.SerialNumber).IsUnique();

        builder.Property(x => x.KitCode).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.KitCode).IsUnique();

        builder.Property(x => x.QrPayload).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.ProductCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.BatchCode).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.ManufacturedAt).IsRequired();
        builder.Property(x => x.ActivatedAt);
        builder.Property(x => x.ExpiresAt);
        builder.Property(x => x.RenewalCount).HasDefaultValue(0);
        builder.Property(x => x.RevokeReason).HasMaxLength(500);
        builder.Property(x => x.RevokedAt);

        // CRITICAL: Computed properties — NOT mapped to database
        builder.Ignore(x => x.EfficiencyPercent);
        builder.Ignore(x => x.DaysUntilExpiry);

        // ── Commercial foundation (Phase 0, July 2026) ────────────────────────
        builder.Property(x => x.ProviderProfileId);
        builder.Property(x => x.SalesChannel).HasConversion<int>();         // null = unattributed
        builder.Property(x => x.CommercialModel).HasConversion<int>();      // null until attributed
        builder.Property(x => x.StockLocationType).HasConversion<int>()
            .IsRequired()
            .HasDefaultValue(StockLocationType.PlatformWarehouse);
        builder.Property(x => x.InvoiceId);                                 // cross-module, no FK
        builder.Property(x => x.PaymentTransactionId);                      // cross-module, no FK

        builder.HasIndex(x => new { x.OwnerUserId, x.Status });
        builder.HasIndex(x => new { x.VesselId, x.Status });
        builder.HasIndex(x => new { x.BatchCode, x.Status });
        builder.HasIndex(x => new { x.ProductCode, x.Status });
        builder.HasIndex(x => x.ExpiresAt);
        builder.HasIndex(x => x.ActivatedAt);
        // Phase 0 indexes for provider attribution and commercial reporting
        builder.HasIndex(x => x.ProviderProfileId);
        builder.HasIndex(x => new { x.SalesChannel, x.Status });
        // CreateDate / ModifyDate: from AizenEntityWithAudit
    }
}
