using Aizen.Modules.CargoDry.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.CargoDry.Repository.Persistence.Configurations;

public sealed class CargoDrySellThroughSettlementEntityConfiguration
    : IEntityTypeConfiguration<CargoDrySellThroughSettlementEntity>
{
    public void Configure(EntityTypeBuilder<CargoDrySellThroughSettlementEntity> builder)
    {
        builder.ToTable("sell_through_settlements");
        builder.HasKey(x => x.Id);

        // ── Identity ───────────────────────────────────────────────────────────
        builder.Property(x => x.SettlementCode).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.SettlementCode).IsUnique();

        // ── Scope ──────────────────────────────────────────────────────────────
        builder.Property(x => x.ConsignmentAgreementId).IsRequired();
        builder.Property(x => x.ProviderProfileId).IsRequired();
        builder.Property(x => x.ProductCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.BatchCode).HasMaxLength(100);

        // ── Kit counts ─────────────────────────────────────────────────────────
        builder.Property(x => x.TotalKitCount).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.SettledKitCount).IsRequired().HasDefaultValue(0);

        // ── Financials ─────────────────────────────────────────────────────────
        builder.Property(x => x.TotalSaleAmount).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(x => x.TotalCommissionAmount).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(x => x.ProviderPayoutAmount).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();

        // ── Period ─────────────────────────────────────────────────────────────
        builder.Property(x => x.PeriodStartUtc).IsRequired();
        builder.Property(x => x.PeriodEndUtc).IsRequired();

        // ── Status ─────────────────────────────────────────────────────────────
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();

        // ── Settlement audit ───────────────────────────────────────────────────
        builder.Property(x => x.ScheduledSettlementDate);
        builder.Property(x => x.SettledAtUtc);
        builder.Property(x => x.SettledByUserId);
        builder.Property(x => x.DisputeReason).HasMaxLength(1000);
        builder.Property(x => x.Note).HasMaxLength(1000);

        // ── Audit ──────────────────────────────────────────────────────────────
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        // ── Query indexes ──────────────────────────────────────────────────────
        builder.HasIndex(x => x.ConsignmentAgreementId);
        builder.HasIndex(x => x.ProviderProfileId);
        builder.HasIndex(x => x.ProductCode);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.PeriodStartUtc);
        builder.HasIndex(x => x.PeriodEndUtc);
        builder.HasIndex(new[]
        {
            nameof(CargoDrySellThroughSettlementEntity.ConsignmentAgreementId),
            nameof(CargoDrySellThroughSettlementEntity.ProductCode),
            nameof(CargoDrySellThroughSettlementEntity.Status),
        });
    }
}
