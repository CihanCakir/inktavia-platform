using Aizen.Modules.CargoDry.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.CargoDry.Repository.Persistence.Configurations;

public sealed class CargoDryProviderInventoryEntityConfiguration
    : IEntityTypeConfiguration<CargoDryProviderInventoryEntity>
{
    public void Configure(EntityTypeBuilder<CargoDryProviderInventoryEntity> builder)
    {
        builder.ToTable("provider_inventories");
        builder.HasKey(x => x.Id);

        // ── Identity ───────────────────────────────────────────────────────────
        builder.Property(x => x.ProviderProfileId).IsRequired();
        builder.Property(x => x.ProductCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.BatchCode).HasMaxLength(100);

        // ── Commercial context ─────────────────────────────────────────────────
        builder.Property(x => x.CommercialModel).HasConversion<int>().IsRequired();
        builder.Property(x => x.SalesChannel).HasConversion<int>().IsRequired();
        builder.Property(x => x.StockLocationType).HasConversion<int>().IsRequired();

        // ── Stock counters ─────────────────────────────────────────────────────
        builder.Property(x => x.TotalAllocated).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.TotalActivated).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.TotalRevoked).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.TotalReturned).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.TotalAdjusted).IsRequired().HasDefaultValue(0);

        // ── Tracking ───────────────────────────────────────────────────────────
        builder.Property(x => x.LastMovementAtUtc);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc);

        // ── Computed — NOT mapped ──────────────────────────────────────────────
        builder.Ignore(x => x.AvailableStock);

        // ── Indexes ────────────────────────────────────────────────────────────
        builder.HasIndex(x => x.ProviderProfileId);
        builder.HasIndex(x => x.ProductCode);
        builder.HasIndex(x => x.BatchCode);
        builder.HasIndex(x => x.CommercialModel);
        builder.HasIndex(x => x.SalesChannel);
        builder.HasIndex(new[] { nameof(CargoDryProviderInventoryEntity.ProviderProfileId),
                                 nameof(CargoDryProviderInventoryEntity.ProductCode),
                                 nameof(CargoDryProviderInventoryEntity.BatchCode) })
               .IsUnique()
               .HasFilter($"\"{nameof(CargoDryProviderInventoryEntity.BatchCode)}\" IS NOT NULL");
    }
}
