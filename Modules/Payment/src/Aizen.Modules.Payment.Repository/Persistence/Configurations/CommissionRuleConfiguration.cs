using Aizen.Modules.Payment.Domain.Entities.Commission;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Payment.Repository.Persistence.Configurations;

public sealed class CommissionRuleConfiguration : IEntityTypeConfiguration<CommissionRuleEntity>
{
    public void Configure(EntityTypeBuilder<CommissionRuleEntity> b)
    {
        b.ToTable("commission_rules");
        b.HasKey(x => x.Id);

        // ── Core targeting ──────────────────────────────────────────────────
        b.Property(x => x.RuleType).HasConversion<int>().IsRequired();
        b.Property(x => x.CategoryCode).HasMaxLength(100);
        b.Property(x => x.ProviderPlanId);
        b.Property(x => x.ProviderProfileId);
        b.Property(x => x.CommissionRate).HasColumnType("numeric(6,4)").IsRequired();
        b.Property(x => x.EffectiveFrom).IsRequired();
        b.Property(x => x.EffectiveTo);
        b.Property(x => x.Notes).HasMaxLength(500);

        // ── Admin management ────────────────────────────────────────────────
        b.Property(x => x.RuleCode).HasMaxLength(20);
        b.Property(x => x.Status).HasConversion<int>().IsRequired();
        b.Property(x => x.Priority).HasConversion<int>().IsRequired();
        b.Property(x => x.ResolvedAppliedCount).IsRequired().HasDefaultValue(0L);

        // ── CargoDry targeting dimensions (Phase 0, July 2026) ─────────────
        b.Property(x => x.ContextType).HasConversion<int>();     // null = all contexts
        b.Property(x => x.ProductCode).HasMaxLength(50);         // null = all products
        b.Property(x => x.SalesChannel).HasConversion<int>();    // null = all channels

        // ── Phase 5: Rule resolution enrichment ──────────────────────────────
        b.Property(x => x.RuleName).HasMaxLength(200);           // human-readable name for trace
        b.Property(x => x.CurrencyCode).HasMaxLength(10);        // null = any currency
        b.Property(x => x.CommercialModel).HasConversion<int>(); // null = any commercial model

        // ── Indexes ─────────────────────────────────────────────────────────
        b.HasIndex(x => x.RuleType);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.RuleCode).IsUnique().HasFilter("\"RuleCode\" IS NOT NULL");
        b.HasIndex(x => new { x.ContextType, x.SalesChannel, x.Status });  // Phase 0: channel rule resolution queries
    }
}
