using Aizen.Modules.Payment.Domain.Entities.ProfitProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Payment.Repository.Persistence.Configurations;

/// <summary>EF mapping for the insert-only profit-protection evaluation log (BE-P5, §7). Money numeric(18,4); enum int.</summary>
public sealed class ProfitProtectionEvaluationLogConfiguration
    : IEntityTypeConfiguration<ProfitProtectionEvaluationLogEntity>
{
    public void Configure(EntityTypeBuilder<ProfitProtectionEvaluationLogEntity> b)
    {
        b.ToTable("profit_protection_evaluation_logs");
        b.HasKey(x => x.Id);

        b.Property(x => x.PolicyId);
        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        b.Property(x => x.DecisionState).HasConversion<int>().IsRequired();
        b.Property(x => x.AdjustmentReason).HasMaxLength(1000);
        b.Property(x => x.EvaluatedAtUtc).IsRequired();

        foreach (var money in new[]
                 {
                     nameof(ProfitProtectionEvaluationLogEntity.ServiceAmount),
                     nameof(ProfitProtectionEvaluationLogEntity.CustomerTotalAmount),
                     nameof(ProfitProtectionEvaluationLogEntity.ProviderNetAmount),
                     nameof(ProfitProtectionEvaluationLogEntity.RequestedPlatformFundedDiscount),
                     nameof(ProfitProtectionEvaluationLogEntity.RequestedCommissionBenefitCost),
                     nameof(ProfitProtectionEvaluationLogEntity.AppliedPlatformFundedDiscount),
                     nameof(ProfitProtectionEvaluationLogEntity.AppliedCommissionBenefit),
                     nameof(ProfitProtectionEvaluationLogEntity.MaximumSafePlatformFundedDiscount),
                     nameof(ProfitProtectionEvaluationLogEntity.CustomerSideContributionExpected),
                     nameof(ProfitProtectionEvaluationLogEntity.ProviderSideContributionExpected),
                     nameof(ProfitProtectionEvaluationLogEntity.TotalTransactionContributionExpected),
                     nameof(ProfitProtectionEvaluationLogEntity.RequiredCustomerSideContribution),
                     nameof(ProfitProtectionEvaluationLogEntity.RequiredProviderSideContribution),
                     nameof(ProfitProtectionEvaluationLogEntity.RequiredTransactionContribution),
                 })
            b.Property(money).HasColumnType("numeric(18,4)").IsRequired();

        b.HasIndex(x => new { x.CurrencyCode, x.DecisionState, x.EvaluatedAtUtc });
        b.HasIndex(x => x.PolicyId);
    }
}
