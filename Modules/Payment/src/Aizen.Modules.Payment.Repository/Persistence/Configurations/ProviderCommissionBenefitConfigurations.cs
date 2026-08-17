using Aizen.Modules.Payment.Domain.Entities.CommissionBenefit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Payment.Repository.Persistence.Configurations;

/// <summary>EF mapping for provider commission benefit rules (BE-P7). Rates numeric(9,4); money numeric(18,4).</summary>
public sealed class ProviderCommissionBenefitRuleConfiguration
    : IEntityTypeConfiguration<ProviderCommissionBenefitRuleEntity>
{
    public void Configure(EntityTypeBuilder<ProviderCommissionBenefitRuleEntity> b)
    {
        b.ToTable("provider_commission_benefit_rules");
        b.HasKey(x => x.Id);

        b.Property(x => x.RuleCode).HasMaxLength(20);
        b.Property(x => x.RuleName).HasMaxLength(200);
        b.Property(x => x.ProviderProfileId);
        b.Property(x => x.ProviderPlanId);
        b.Property(x => x.ApplicableCategoryCodesCsv).HasMaxLength(1000);
        b.Property(x => x.AdjustmentPercentagePoints).HasColumnType("numeric(9,4)").IsRequired();
        b.Property(x => x.MinimumCommissionRate).HasColumnType("numeric(9,4)").IsRequired();
        b.Property(x => x.MaximumDiscountAmount).HasColumnType("numeric(18,4)");
        b.Property(x => x.MaximumEligibleGMV).HasColumnType("numeric(18,4)");
        b.Property(x => x.UsageLimit);
        b.Property(x => x.Stackable).IsRequired();
        b.Property(x => x.Exclusive).IsRequired();
        b.Property(x => x.Priority).HasConversion<int>().IsRequired();
        b.Property(x => x.EffectiveFrom).IsRequired();
        b.Property(x => x.EffectiveTo);
        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        b.Property(x => x.Status).HasConversion<int>().IsRequired();
        b.Property(x => x.Notes).HasMaxLength(1000);
        b.Ignore(x => x.ApplicableCategoryCodes);

        b.HasIndex(x => x.RuleCode).IsUnique().HasFilter("\"RuleCode\" IS NOT NULL");
        b.HasIndex(x => new { x.CurrencyCode, x.Status, x.EffectiveFrom, x.EffectiveTo });
        b.HasIndex(x => new { x.ProviderProfileId, x.ProviderPlanId });
    }
}

/// <summary>EF mapping for the entitlement (BE-P7). Version is an optimistic-concurrency token; Remaining* are computed.</summary>
public sealed class ProviderCommissionBenefitEntitlementConfiguration
    : IEntityTypeConfiguration<ProviderCommissionBenefitEntitlementEntity>
{
    public void Configure(EntityTypeBuilder<ProviderCommissionBenefitEntitlementEntity> b)
    {
        b.ToTable("provider_commission_benefit_entitlements");
        b.HasKey(x => x.Id);

        b.Property(x => x.EntitlementCode).HasMaxLength(20);
        b.Property(x => x.ProviderProfileId).IsRequired();
        b.Property(x => x.BenefitRuleId).IsRequired();
        b.Property(x => x.GrantedFrom).IsRequired();
        b.Property(x => x.GrantedTo);
        b.Property(x => x.UsageLimit);
        b.Property(x => x.UsedCount).IsRequired();
        b.Property(x => x.ReservedCount).IsRequired();
        b.Property(x => x.MaximumEligibleGMV).HasColumnType("numeric(18,4)");
        b.Property(x => x.ConsumedGMV).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.ReservedGMV).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.Status).HasConversion<int>().IsRequired();
        b.Property(x => x.Version).IsConcurrencyToken().IsRequired();

        b.Ignore(x => x.RemainingUsage);
        b.Ignore(x => x.RemainingGmv);

        b.HasIndex(x => x.EntitlementCode).IsUnique().HasFilter("\"EntitlementCode\" IS NOT NULL");
        b.HasIndex(x => new { x.ProviderProfileId, x.BenefitRuleId, x.Status });
    }
}

/// <summary>EF mapping for the usage ledger (BE-P7). Unique (EntitlementId, ContextRef) blocks double-reserve per context.</summary>
public sealed class ProviderCommissionBenefitUsageConfiguration
    : IEntityTypeConfiguration<ProviderCommissionBenefitUsageEntity>
{
    public void Configure(EntityTypeBuilder<ProviderCommissionBenefitUsageEntity> b)
    {
        b.ToTable("provider_commission_benefit_usages");
        b.HasKey(x => x.Id);

        b.Property(x => x.EntitlementId).IsRequired();
        b.Property(x => x.ContextRef).HasMaxLength(200).IsRequired();
        b.Property(x => x.GmvAmount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.BenefitAmount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.Status).HasConversion<int>().IsRequired();
        b.Property(x => x.ReservedAtUtc).IsRequired();
        b.Property(x => x.ConsumedAtUtc);
        b.Property(x => x.ReleasedAtUtc);

        b.HasIndex(x => new { x.EntitlementId, x.ContextRef }).IsUnique();
        b.HasIndex(x => x.Status);
    }
}
