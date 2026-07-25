using Aizen.Modules.CargoDry.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.CargoDry.Repository.Persistence.Configurations;

public sealed class CargoDrySalesAttributionEntityConfiguration
    : IEntityTypeConfiguration<CargoDrySalesAttributionEntity>
{
    public void Configure(EntityTypeBuilder<CargoDrySalesAttributionEntity> builder)
    {
        builder.ToTable("sales_attributions");
        builder.HasKey(x => x.Id);

        // ── Kit identity ───────────────────────────────────────────────────────
        builder.Property(x => x.KitId).IsRequired();
        builder.Property(x => x.SerialNumber).HasMaxLength(100).IsRequired();
        builder.Property(x => x.KitCode).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ProductCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.BatchCode).HasMaxLength(100);

        // ── Commercial context ─────────────────────────────────────────────────
        builder.Property(x => x.ProviderProfileId);
        builder.Property(x => x.SalesChannel).HasConversion<int>().IsRequired();
        builder.Property(x => x.CommercialModel).HasConversion<int>().IsRequired();
        builder.Property(x => x.ConsignmentAgreementId);
        builder.Property(x => x.InventoryId);

        // ── Status ─────────────────────────────────────────────────────────────
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();

        // ── Financials ─────────────────────────────────────────────────────────
        builder.Property(x => x.SalePrice).HasColumnType("numeric(18,4)");
        builder.Property(x => x.CommissionRate).HasColumnType("numeric(8,4)");
        builder.Property(x => x.CommissionAmount).HasColumnType("numeric(18,4)");
        builder.Property(x => x.CurrencyCode).HasMaxLength(3);

        // ── Extended financials (Phase 4A) ─────────────────────────────────────
        builder.Property(x => x.ProviderShareAmount).HasColumnType("numeric(18,4)");
        builder.Property(x => x.PlatformShareAmount).HasColumnType("numeric(18,4)");
        builder.Property(x => x.FinancialResolvedAtUtc);
        builder.Property(x => x.FinancialResolvedByUserId);
        builder.Property(x => x.ResolutionNote).HasMaxLength(1000);
        builder.Ignore(x => x.IsFinanciallyResolved); // computed — not stored

        // ── Settlement link ────────────────────────────────────────────────────
        builder.Property(x => x.SellThroughSettlementId);

        // ── Phase 5: Commercial rule trace ────────────────────────────────────
        builder.Property(x => x.ResolvedRuleId);
        builder.Property(x => x.ResolvedRuleSource).HasMaxLength(100);
        builder.Property(x => x.ResolvedRuleName).HasMaxLength(250);
        builder.Property(x => x.ResolvedRate).HasColumnType("numeric(8,4)");
        builder.Property(x => x.RateResolvedAtUtc);
        builder.Property(x => x.RateResolvedByUserId);
        builder.Property(x => x.RuleResolutionNote).HasMaxLength(1000);

        // ── CE-6a-(b): Tier snapshot ──────────────────────────────────────────
        builder.Property(x => x.TierAtSale).HasMaxLength(20);
        builder.Property(x => x.TierBonusRate).HasColumnType("numeric(8,4)").HasDefaultValue(0m).IsRequired();

        // ── Attribution audit ──────────────────────────────────────────────────
        builder.Property(x => x.AttributedAt);
        builder.Property(x => x.AttributedByUserId);

        // ── Review ─────────────────────────────────────────────────────────────
        builder.Property(x => x.ReviewNote).HasMaxLength(1000);
        builder.Property(x => x.ReviewedByUserId);
        builder.Property(x => x.ReviewedAt);

        // ── Audit ──────────────────────────────────────────────────────────────
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        // ── Unique constraint — one attribution per kit ───────────────────────
        builder.HasIndex(x => x.KitId).IsUnique();

        // ── Query indexes ──────────────────────────────────────────────────────
        builder.HasIndex(x => x.ProviderProfileId);
        builder.HasIndex(x => x.ProductCode);
        builder.HasIndex(x => x.BatchCode);
        builder.HasIndex(x => x.SalesChannel);
        builder.HasIndex(x => x.CommercialModel);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.SellThroughSettlementId);
        builder.HasIndex(x => x.ConsignmentAgreementId);
        builder.HasIndex(x => x.CreatedAtUtc);
    }
}
