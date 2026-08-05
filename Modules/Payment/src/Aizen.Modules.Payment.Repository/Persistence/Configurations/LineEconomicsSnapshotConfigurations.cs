using Aizen.Modules.Payment.Domain.Entities.Economics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Payment.Repository.Persistence.Configurations;

/// <summary>
/// EF mapping for the immutable per-line economics snapshot (§20.15). Money numeric(18,4); rate numeric(9,4);
/// item type / pricing method / eligibility stored as int. FK → payment_economics_snapshots (OnDelete Restrict,
/// configured on the aggregate). Lookup index on (EconomicsSnapshotId) and (EconomicsSnapshotId, LineRef).
/// </summary>
public sealed class OfferLineEconomicsSnapshotConfiguration : IEntityTypeConfiguration<OfferLineEconomicsSnapshotEntity>
{
    public void Configure(EntityTypeBuilder<OfferLineEconomicsSnapshotEntity> b)
    {
        b.ToTable("offer_line_economics_snapshots");
        b.HasKey(x => x.Id);

        b.Property(x => x.EconomicsSnapshotId).IsRequired();
        b.Property(x => x.LineRef).HasMaxLength(100).IsRequired();
        b.Property(x => x.ItemType).IsRequired();
        b.Property(x => x.PricingMethod).IsRequired();
        b.Property(x => x.LineGrossBeforeDiscount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.CustomerDiscountAmount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.ProviderFundedDiscountAmount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.PlatformFundedDiscountAmount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.CommissionEligibility).HasConversion<int>().IsRequired();
        b.Property(x => x.CommissionBaseAmount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.CommissionRate).HasColumnType("numeric(9,4)").IsRequired();
        b.Property(x => x.CommissionAmount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.ProviderNetAmount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.LineVatAmount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.LineTotalAmount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        b.Property(x => x.SortOrder).IsRequired();

        // ── S3 frozen FX metadata (§20.7) — all nullable (settlement-native lines = null; back-compat for existing rows) ──
        b.Property(x => x.FxSourceCurrencyCode).HasMaxLength(10);
        b.Property(x => x.FxSettlementCurrencyCode).HasMaxLength(10);
        b.Property(x => x.FxSourceUnitPrice).HasColumnType("numeric(18,4)");
        b.Property(x => x.FxAppliedRate).HasColumnType("numeric(18,6)");
        b.Property(x => x.FxResolvedUnitPrice).HasColumnType("numeric(18,4)");

        b.HasIndex(x => x.EconomicsSnapshotId);
        b.HasIndex(x => new { x.EconomicsSnapshotId, x.LineRef });

        // S2d — attribute snapshots are children of THIS line snapshot (FK, OnDelete Restrict).
        b.HasMany(x => x.AttributeSnapshots)
            .WithOne()
            .HasForeignKey(a => a.OfferLineEconomicsSnapshotId)
            .OnDelete(DeleteBehavior.Restrict);
        b.Metadata.FindNavigation(nameof(OfferLineEconomicsSnapshotEntity.AttributeSnapshots))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

/// <summary>
/// S2d — EF mapping for the immutable per-attribute line snapshot (§20.6/§20.15). FK → offer_line_economics_snapshots
/// (OnDelete Restrict, configured on the line side). Descriptive metadata, not part of the money math.
/// </summary>
public sealed class OfferLineAttributeSnapshotConfiguration : IEntityTypeConfiguration<OfferLineAttributeSnapshotEntity>
{
    public void Configure(EntityTypeBuilder<OfferLineAttributeSnapshotEntity> b)
    {
        b.ToTable("offer_line_attribute_snapshots");
        b.HasKey(x => x.Id);

        b.Property(x => x.OfferLineEconomicsSnapshotId).IsRequired();
        b.Property(x => x.DefinitionCode).HasMaxLength(100).IsRequired();
        b.Property(x => x.DataType).IsRequired();
        b.Property(x => x.ValueLookupItemCode).HasMaxLength(100);
        b.Property(x => x.ValueLookupItemLabel).HasMaxLength(200);
        b.Property(x => x.ValueNumber).HasColumnType("numeric(18,4)");
        b.Property(x => x.ValueText).HasMaxLength(1000);
        b.Property(x => x.SortOrder).IsRequired();

        b.HasIndex(x => x.OfferLineEconomicsSnapshotId);
        b.HasIndex(x => new { x.OfferLineEconomicsSnapshotId, x.DefinitionCode });
    }
}

/// <summary>EF mapping for the immutable per-line commission-rule audit (§20.15).</summary>
public sealed class CommissionAllocationSnapshotConfiguration : IEntityTypeConfiguration<CommissionAllocationSnapshotEntity>
{
    public void Configure(EntityTypeBuilder<CommissionAllocationSnapshotEntity> b)
    {
        b.ToTable("commission_allocation_snapshots");
        b.HasKey(x => x.Id);

        b.Property(x => x.EconomicsSnapshotId).IsRequired();
        b.Property(x => x.LineRef).HasMaxLength(100).IsRequired();
        b.Property(x => x.CommissionRuleId);                       // null when eligibility exempt / no rule
        b.Property(x => x.CommissionRuleCode).HasMaxLength(100);
        b.Property(x => x.CommissionBaseAmount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.ResolvedRate).HasColumnType("numeric(9,4)").IsRequired();
        b.Property(x => x.CommissionAmount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.Commissionable).IsRequired();

        b.HasIndex(x => x.EconomicsSnapshotId);
        b.HasIndex(x => new { x.EconomicsSnapshotId, x.LineRef });
    }
}

/// <summary>EF mapping for the immutable per-line discount funding allocation (§20.15) — 0 rows in the narrow core.</summary>
public sealed class DiscountAllocationSnapshotConfiguration : IEntityTypeConfiguration<DiscountAllocationSnapshotEntity>
{
    public void Configure(EntityTypeBuilder<DiscountAllocationSnapshotEntity> b)
    {
        b.ToTable("discount_allocation_snapshots");
        b.HasKey(x => x.Id);

        b.Property(x => x.EconomicsSnapshotId).IsRequired();
        b.Property(x => x.LineRef).HasMaxLength(100).IsRequired();
        b.Property(x => x.FundingSource).HasConversion<int>().IsRequired();
        b.Property(x => x.DiscountAmount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.RuleCode).HasMaxLength(100);

        b.HasIndex(x => x.EconomicsSnapshotId);
        b.HasIndex(x => new { x.EconomicsSnapshotId, x.LineRef });
    }
}
