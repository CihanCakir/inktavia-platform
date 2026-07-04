using Aizen.Modules.CargoDry.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.CargoDry.Repository.Persistence.Configurations;

public sealed class CargoDrySettlementAutomationRunItemEntityConfiguration
    : IEntityTypeConfiguration<CargoDrySettlementAutomationRunItemEntity>
{
    public void Configure(EntityTypeBuilder<CargoDrySettlementAutomationRunItemEntity> builder)
    {
        builder.ToTable("settlement_automation_run_items");
        builder.HasKey(x => x.Id);

        // ── FK ─────────────────────────────────────────────────────────────────
        // Configured via parent navigation (HasMany → WithOne → HasForeignKey)
        builder.Property(x => x.RunId).IsRequired();

        // ── Settlement reference (denormalized, no EF FK across module boundary) ─
        builder.Property(x => x.SettlementId).IsRequired();
        builder.Property(x => x.SettlementCode).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ProviderProfileId).IsRequired();
        builder.Property(x => x.ProductCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(x => x.PeriodYearMonth).IsRequired();

        // ── State ──────────────────────────────────────────────────────────────
        builder.Property(x => x.StatusBefore).HasConversion<int>().IsRequired();

        // ── Outcome ────────────────────────────────────────────────────────────
        builder.Property(x => x.Action).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Success).IsRequired();
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);

        // ── Attribution counters ───────────────────────────────────────────────
        builder.Property(x => x.AttributionsResolved).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.AttributionsSkipped).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.AttributionsErrored).IsRequired().HasDefaultValue(0);

        // ── Timestamps ─────────────────────────────────────────────────────────
        builder.Property(x => x.ProcessedAtUtc).IsRequired();

        // ── Query indexes ──────────────────────────────────────────────────────
        builder.HasIndex(x => x.RunId);
        builder.HasIndex(x => x.SettlementId);
        builder.HasIndex(x => x.ProviderProfileId);
        builder.HasIndex(x => x.Success);
    }
}
