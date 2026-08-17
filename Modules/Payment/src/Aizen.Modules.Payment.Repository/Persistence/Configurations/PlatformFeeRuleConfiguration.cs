using Aizen.Modules.Payment.Domain.Entities.PlatformFee;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Payment.Repository.Persistence.Configurations;

/// <summary>
/// EF mapping for platform fee rules (BE-P3). Money numeric(18,4); rates numeric(9,4); enums as int;
/// unique RuleCode; resolution indexes on (Status, EffectiveFrom, EffectiveTo) and (CurrencyCode, CategoryCode, CustomerType).
/// </summary>
public sealed class PlatformFeeRuleConfiguration : IEntityTypeConfiguration<PlatformFeeRuleEntity>
{
    public void Configure(EntityTypeBuilder<PlatformFeeRuleEntity> b)
    {
        b.ToTable("platform_fee_rules");
        b.HasKey(x => x.Id);

        // ── Model + amounts ─────────────────────────────────────────────────────
        b.Property(x => x.Model).HasConversion<int>().IsRequired();
        b.Property(x => x.Rate).HasColumnType("numeric(9,4)");
        b.Property(x => x.FixedAmount).HasColumnType("numeric(18,4)");
        b.Property(x => x.MinAmount).HasColumnType("numeric(18,4)");
        b.Property(x => x.MaxAmount).HasColumnType("numeric(18,4)");
        b.Property(x => x.VatRate).HasColumnType("numeric(9,4)");

        // ── Targeting ───────────────────────────────────────────────────────────
        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        b.Property(x => x.CategoryCode).HasMaxLength(100);
        b.Property(x => x.CustomerType).HasMaxLength(50);

        // ── Lifecycle / admin ───────────────────────────────────────────────────
        b.Property(x => x.Priority).HasConversion<int>().IsRequired();
        b.Property(x => x.Status).HasConversion<int>().IsRequired();
        b.Property(x => x.EffectiveFrom).IsRequired();
        b.Property(x => x.EffectiveTo);
        b.Property(x => x.RuleCode).HasMaxLength(20);
        b.Property(x => x.RuleName).HasMaxLength(200);
        b.Property(x => x.Notes).HasMaxLength(1000);

        // ── Indexes ─────────────────────────────────────────────────────────────
        b.HasIndex(x => x.RuleCode).IsUnique().HasFilter("\"RuleCode\" IS NOT NULL");
        b.HasIndex(x => new { x.Status, x.EffectiveFrom, x.EffectiveTo });
        b.HasIndex(x => new { x.CurrencyCode, x.CategoryCode, x.CustomerType });
    }
}
