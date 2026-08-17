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
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.ValidityDays).IsRequired();
        builder.Property(x => x.HasSmartDevice).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeviceType).HasMaxLength(100);
        builder.Property(x => x.RetailPrice).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        // ── Commercial pricing (Phase 0, July 2026) ───────────────────────────
        builder.Property(x => x.WholesalePrice).HasColumnType("decimal(18,2)");         // null = no resale configured
        builder.Property(x => x.ConsignmentPrice).HasColumnType("decimal(18,2)");       // null = use RetailPrice as fallback
        builder.Property(x => x.ProviderCommissionRate).HasColumnType("decimal(6,4)");  // 0.00-1.00; null = no commission

        // IsActive: mapped by AizenEntityWithAudit base configuration
        // CreateDate / ModifyDate: mapped by AizenEntityWithAudit base configuration — DO NOT re-map
    }
}
