using Aizen.Modules.Payment.Domain.Entities.CustomerBenefit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Payment.Repository.Persistence.Configurations;

/// <summary>EF mapping for the benefit budget (BE-P6). Version is an optimistic-concurrency token; Remaining is computed.</summary>
public sealed class CustomerBenefitBudgetConfiguration : IEntityTypeConfiguration<CustomerBenefitBudgetEntity>
{
    public void Configure(EntityTypeBuilder<CustomerBenefitBudgetEntity> b)
    {
        b.ToTable("customer_benefit_budgets");
        b.HasKey(x => x.Id);

        b.Property(x => x.ParticipantPlanSubscriptionId).IsRequired();
        b.Property(x => x.CustomerPlanId).IsRequired();
        b.Property(x => x.PeriodStart).IsRequired();
        b.Property(x => x.PeriodEnd).IsRequired();
        b.Property(x => x.FundedAmount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.ReservedAmount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.ConsumedAmount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        b.Property(x => x.Status).HasConversion<int>().IsRequired();

        // Optimistic concurrency — prevents two concurrent checkouts double-spending the same budget (§8/§19.15).
        b.Property(x => x.Version).IsConcurrencyToken().IsRequired();
        b.Property(x => x.LowBudgetNotified).IsRequired().HasDefaultValue(false);   // N4 once-per-crossing marker
        b.Ignore(x => x.IsExhausted);   // computed

        b.Ignore(x => x.RemainingAmount);   // computed

        b.HasIndex(x => new { x.ParticipantPlanSubscriptionId, x.Status });
    }
}

/// <summary>EF mapping for benefit reservations (BE-P6). Unique (BudgetId, ContextRef) blocks double-reserve per context.</summary>
public sealed class CustomerBenefitReservationConfiguration : IEntityTypeConfiguration<CustomerBenefitReservationEntity>
{
    public void Configure(EntityTypeBuilder<CustomerBenefitReservationEntity> b)
    {
        b.ToTable("customer_benefit_reservations");
        b.HasKey(x => x.Id);

        b.Property(x => x.BudgetId).IsRequired();
        b.Property(x => x.Amount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.Status).HasConversion<int>().IsRequired();
        b.Property(x => x.ContextRef).HasMaxLength(200).IsRequired();
        b.Property(x => x.ReservedAtUtc).IsRequired();
        b.Property(x => x.ConsumedAtUtc);
        b.Property(x => x.ReleasedAtUtc);

        b.HasIndex(x => new { x.BudgetId, x.ContextRef }).IsUnique();
        b.HasIndex(x => x.Status);
    }
}

/// <summary>EF mapping for benefit budget policies (BE-P6). Rate numeric(9,4); money numeric(18,4); unique PolicyCode.</summary>
public sealed class CustomerBenefitBudgetPolicyConfiguration : IEntityTypeConfiguration<CustomerBenefitBudgetPolicyEntity>
{
    public void Configure(EntityTypeBuilder<CustomerBenefitBudgetPolicyEntity> b)
    {
        b.ToTable("customer_benefit_budget_policies");
        b.HasKey(x => x.Id);

        b.Property(x => x.CustomerPlanId).IsRequired();
        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        b.Property(x => x.BenefitBudgetRate).HasColumnType("numeric(9,4)").IsRequired();
        b.Property(x => x.PerPeriodMax).HasColumnType("numeric(18,4)");
        b.Property(x => x.PerCategoryLimit).HasColumnType("numeric(18,4)");
        b.Property(x => x.PerTransactionLimit).HasColumnType("numeric(18,4)");
        b.Property(x => x.RefundRestorePolicy).HasConversion<int>().IsRequired();
        b.Property(x => x.EffectiveFrom).IsRequired();
        b.Property(x => x.EffectiveTo);
        b.Property(x => x.Status).HasConversion<int>().IsRequired();
        b.Property(x => x.PolicyCode).HasMaxLength(20);
        b.Property(x => x.Notes).HasMaxLength(1000);

        b.HasIndex(x => x.PolicyCode).IsUnique().HasFilter("\"PolicyCode\" IS NOT NULL");
        b.HasIndex(x => new { x.CustomerPlanId, x.CurrencyCode, x.Status });
    }
}
