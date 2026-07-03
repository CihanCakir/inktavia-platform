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

        // ── Settlement link ────────────────────────────────────────────────────
        builder.Property(x => x.SellThroughSettlementId);

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
