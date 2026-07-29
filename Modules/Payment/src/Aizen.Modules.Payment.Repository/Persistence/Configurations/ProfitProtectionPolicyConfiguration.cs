using Aizen.Modules.Payment.Domain.Entities.ProfitProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Payment.Repository.Persistence.Configurations;

/// <summary>
/// EF mapping for profit-protection policies (BE-P5). Amounts numeric(18,4); rates numeric(9,4); enums int;
/// unique filtered PolicyCode; effective index (CurrencyCode, Status, EffectiveFrom, EffectiveTo).
/// </summary>
public sealed class ProfitProtectionPolicyConfiguration : IEntityTypeConfiguration<ProfitProtectionPolicyEntity>
{
    public void Configure(EntityTypeBuilder<ProfitProtectionPolicyEntity> b)
    {
        b.ToTable("profit_protection_policies");
        b.HasKey(x => x.Id);

        foreach (var amount in new[]
                 {
                     nameof(ProfitProtectionPolicyEntity.MinCustomerSideContributionAmount),
                     nameof(ProfitProtectionPolicyEntity.MinProviderSideContributionAmount),
                     nameof(ProfitProtectionPolicyEntity.MinTransactionContributionAmount),
                     nameof(ProfitProtectionPolicyEntity.PaymentProcessingFixed),
                     nameof(ProfitProtectionPolicyEntity.OtherVariableExpenseFixed),
                 })
            b.Property(amount).HasColumnType("numeric(18,4)").IsRequired();

        foreach (var rate in new[]
                 {
                     nameof(ProfitProtectionPolicyEntity.MinCustomerSideContributionRate),
                     nameof(ProfitProtectionPolicyEntity.MinProviderSideContributionRate),
                     nameof(ProfitProtectionPolicyEntity.MinTransactionContributionRate),
                     nameof(ProfitProtectionPolicyEntity.PaymentProcessingExpenseRate),
                     nameof(ProfitProtectionPolicyEntity.RefundRiskReserveRate),
                     nameof(ProfitProtectionPolicyEntity.OtherVariableExpenseRate),
                     nameof(ProfitProtectionPolicyEntity.CustomerSideVariableCostShareRate),
                 })
            b.Property(rate).HasColumnType("numeric(9,4)").IsRequired();

        b.Property(x => x.AdjustmentOrder).HasConversion<int>().IsRequired();
        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        b.Property(x => x.EffectiveFrom).IsRequired();
        b.Property(x => x.EffectiveTo);
        b.Property(x => x.Status).HasConversion<int>().IsRequired();
        b.Property(x => x.PolicyCode).HasMaxLength(20);
        b.Property(x => x.PolicyName).HasMaxLength(200);
        b.Property(x => x.Notes).HasMaxLength(1000);

        b.HasIndex(x => x.PolicyCode).IsUnique().HasFilter("\"PolicyCode\" IS NOT NULL");
        b.HasIndex(x => new { x.CurrencyCode, x.Status, x.EffectiveFrom, x.EffectiveTo });
    }
}
