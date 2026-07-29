using Aizen.Modules.Payment.Domain.Entities.Economics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Payment.Repository.Persistence.Configurations;

/// <summary>
/// EF mapping for the immutable economics snapshot (§5). Money is numeric(18,4); rates numeric(9,4);
/// the context enum is stored as int. Unique index on SnapshotCode; lookup index on (ContextType, ContextId).
/// </summary>
public sealed class PaymentEconomicsSnapshotConfiguration : IEntityTypeConfiguration<PaymentEconomicsSnapshotEntity>
{
    public void Configure(EntityTypeBuilder<PaymentEconomicsSnapshotEntity> b)
    {
        b.ToTable("payment_economics_snapshots");
        b.HasKey(x => x.Id);

        // ── Identity / context ──────────────────────────────────────────────────
        b.Property(x => x.SnapshotCode).HasMaxLength(30).IsRequired();
        b.HasIndex(x => x.SnapshotCode).IsUnique();
        b.Property(x => x.ContextType).HasConversion<int>().IsRequired();
        b.Property(x => x.ContextId).IsRequired();
        b.Property(x => x.CurrencyCodeSnapshot).HasMaxLength(10).IsRequired();
        b.Property(x => x.RoundingModeSnapshot).HasMaxLength(30).IsRequired();
        b.Property(x => x.CreatedAtUtc).IsRequired();

        // ── Service (net / vat / gross) ─────────────────────────────────────────
        b.Property(x => x.ServiceAmountSnapshot).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.ServiceVatAmountSnapshot).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.ServiceGrossAmountSnapshot).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.CustomerPayableServiceAmountSnapshot).HasColumnType("numeric(18,4)").IsRequired();

        // ── Commission ──────────────────────────────────────────────────────────
        b.Property(x => x.CommissionBaseAmountSnapshot).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.CommissionRateSnapshot).HasColumnType("numeric(9,4)").IsRequired();
        b.Property(x => x.CommissionAmountSnapshot).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.ProviderNetAmountSnapshot).HasColumnType("numeric(18,4)").IsRequired();

        // ── Platform fee (net / vat / gross + resolved bounds) ─────────────────
        b.Property(x => x.PlatformFeeBaseAmountSnapshot).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.PlatformFeeRuleIdSnapshot);                                  // null until P3 rule engine
        b.Property(x => x.PlatformFeeRateSnapshot).HasColumnType("numeric(9,4)").IsRequired();
        b.Property(x => x.PlatformFeeMinimumSnapshot).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.PlatformFeeMaximumSnapshot).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.PlatformFeeNetAmountSnapshot).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.PlatformFeeVatAmountSnapshot).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.PlatformFeeGrossAmountSnapshot).HasColumnType("numeric(18,4)").IsRequired();

        // ── Totals (book share only, §13.10) ────────────────────────────────────
        b.Property(x => x.CustomerTotalAmountSnapshot).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.PlatformGrossShareSnapshot).HasColumnType("numeric(18,4)").IsRequired();

        // ── §20.15 aggregate decomposition (BE-S8) — legacy rows default 0 ──────
        b.Property(x => x.OriginalServiceGrossAmountSnapshot).HasColumnType("numeric(18,4)").IsRequired().HasDefaultValue(0m);
        b.Property(x => x.TotalCustomerDiscountSnapshot).HasColumnType("numeric(18,4)").IsRequired().HasDefaultValue(0m);
        b.Property(x => x.TotalProviderFundedDiscountSnapshot).HasColumnType("numeric(18,4)").IsRequired().HasDefaultValue(0m);
        b.Property(x => x.TotalPlatformFundedDiscountSnapshot).HasColumnType("numeric(18,4)").IsRequired().HasDefaultValue(0m);
        b.Property(x => x.ServiceVatTotalSnapshot).HasColumnType("numeric(18,4)").IsRequired().HasDefaultValue(0m);

        // ── Line snapshot children (BE-S8) — insert-only, FK OnDelete Restrict ──
        b.HasMany(x => x.OfferLines).WithOne().HasForeignKey(l => l.EconomicsSnapshotId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.CommissionAllocations).WithOne().HasForeignKey(l => l.EconomicsSnapshotId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.DiscountAllocations).WithOne().HasForeignKey(l => l.EconomicsSnapshotId).OnDelete(DeleteBehavior.Restrict);
        b.Metadata.FindNavigation(nameof(PaymentEconomicsSnapshotEntity.OfferLines))!.SetPropertyAccessMode(PropertyAccessMode.Field);
        b.Metadata.FindNavigation(nameof(PaymentEconomicsSnapshotEntity.CommissionAllocations))!.SetPropertyAccessMode(PropertyAccessMode.Field);
        b.Metadata.FindNavigation(nameof(PaymentEconomicsSnapshotEntity.DiscountAllocations))!.SetPropertyAccessMode(PropertyAccessMode.Field);

        // ── Indexes ─────────────────────────────────────────────────────────────
        b.HasIndex(x => new { x.ContextType, x.ContextId });
    }
}
