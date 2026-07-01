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

        // ── Indexes ─────────────────────────────────────────────────────────
        b.HasIndex(x => x.RuleType);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.RuleCode).IsUnique().HasFilter("\"RuleCode\" IS NOT NULL");
    }
}
