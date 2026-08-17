using Aizen.Modules.CargoDry.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.CargoDry.Repository.Persistence.Configurations;

public sealed class CargoDrySettlementAutomationRunEntityConfiguration
    : IEntityTypeConfiguration<CargoDrySettlementAutomationRunEntity>
{
    public void Configure(EntityTypeBuilder<CargoDrySettlementAutomationRunEntity> builder)
    {
        builder.ToTable("settlement_automation_runs");
        builder.HasKey(x => x.Id);

        // ── Identity ───────────────────────────────────────────────────────────
        // Format: SAR-{YYYYMM}-{seq:D4}, e.g. "SAR-202606-0001"
        builder.Property(x => x.RunCode).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.RunCode).IsUnique();

        // ── Run configuration ──────────────────────────────────────────────────
        builder.Property(x => x.TargetYearMonth).IsRequired();
        builder.Property(x => x.Mode).HasConversion<int>().IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();

        // ── Safety flags ───────────────────────────────────────────────────────
        builder.Property(x => x.AutoCompletePayout).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.AutoPreparePayment).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.AutoPrepareInvoice).IsRequired().HasDefaultValue(false);

        // ── Trigger ────────────────────────────────────────────────────────────
        builder.Property(x => x.TriggeredByUserId).IsRequired();
        builder.Property(x => x.TriggeredAtUtc).IsRequired();
        builder.Property(x => x.CompletedAtUtc);
        builder.Property(x => x.DurationMs);

        // ── Aggregate counters ─────────────────────────────────────────────────
        builder.Property(x => x.TotalSettlementsFound).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.TotalSettlementsEligible).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.TotalSettlementsProcessed).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.TotalSettlementsSkipped).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.TotalSettlementsErrored).IsRequired().HasDefaultValue(0);

        // ── Notes ──────────────────────────────────────────────────────────────
        builder.Property(x => x.Note).HasMaxLength(1000);
        builder.Property(x => x.ErrorSummary).HasMaxLength(4000);

        // ── Audit ──────────────────────────────────────────────────────────────
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        // ── Navigation: run items ──────────────────────────────────────────────
        builder.HasMany(x => x.RunItems)
               .WithOne()
               .HasForeignKey(x => x.RunId)
               .OnDelete(DeleteBehavior.Cascade);

        // ── Query indexes ──────────────────────────────────────────────────────
        builder.HasIndex(x => x.TargetYearMonth);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.Mode);
        builder.HasIndex(x => x.TriggeredByUserId);
        builder.HasIndex(x => x.TriggeredAtUtc);
    }
}
