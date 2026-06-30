using Aizen.Modules.Payment.Domain.Entities.Plan;
using Aizen.Modules.Payment.Domain.Entities.Subscription;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Payment.Repository.Persistence.Configurations;

public sealed class ParticipantPlanConfiguration : IEntityTypeConfiguration<ParticipantPlanEntity>
{
    public void Configure(EntityTypeBuilder<ParticipantPlanEntity> b)
    {
        b.ToTable("participant_plans");
        b.HasKey(x => x.Id);
        b.Property(x => x.PlanCode).HasMaxLength(50).IsRequired();
        b.HasIndex(x => x.PlanCode).IsUnique();
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.MonthlyPriceTRY).HasColumnType("numeric(18,2)").IsRequired();
        b.Property(x => x.ServiceDiscountRate).HasColumnType("numeric(6,4)").IsRequired();
        b.Property(x => x.CargoDryDiscountRate).HasColumnType("numeric(6,4)").IsRequired();
        b.Property(x => x.InkCoinEarnMultiplier).HasColumnType("numeric(6,2)").IsRequired();
        b.Property(x => x.SortOrder).IsRequired().HasDefaultValue(0);
    }
}

public sealed class ParticipantPlanSubscriptionConfiguration : IEntityTypeConfiguration<ParticipantPlanSubscriptionEntity>
{
    public void Configure(EntityTypeBuilder<ParticipantPlanSubscriptionEntity> b)
    {
        b.ToTable("participant_plan_subscriptions");
        b.HasKey(x => x.Id);
        b.Property(x => x.ParticipantProfileId).IsRequired();
        b.Property(x => x.ParticipantPlanId).IsRequired();
        b.Property(x => x.Status).HasConversion<int>().IsRequired();
        b.Property(x => x.PaidAmount).HasColumnType("numeric(18,2)").IsRequired();
        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        b.Property(x => x.SubscriptionPeriodStart).IsRequired();
        b.Property(x => x.SubscriptionPeriodEnd).IsRequired();
        b.Property(x => x.ServiceDiscountAtSubscription).HasColumnType("numeric(6,4)").IsRequired();
        b.Property(x => x.EarnMultiplierAtSubscription).HasColumnType("numeric(6,2)").IsRequired();
        b.HasIndex(x => new { x.ParticipantProfileId, x.Status });
    }
}
