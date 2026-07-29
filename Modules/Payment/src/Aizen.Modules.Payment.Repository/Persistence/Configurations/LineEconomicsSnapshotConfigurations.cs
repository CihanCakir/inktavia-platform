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

        b.HasIndex(x => x.EconomicsSnapshotId);
        b.HasIndex(x => new { x.EconomicsSnapshotId, x.LineRef });
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
