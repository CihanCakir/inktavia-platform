using System.Text.Json;
using Aizen.Modules.Payment.Domain.Entities.Plan;
using Aizen.Modules.Payment.Domain.Entities.Subscription;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Payment.Repository.Persistence.Configurations;

public sealed class ProviderPlanConfiguration : IEntityTypeConfiguration<ProviderPlanEntity>
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<ProviderPlanEntity> b)
    {
        b.ToTable("provider_plans");
        b.HasKey(x => x.Id);
        b.Property(x => x.PlanCode).HasMaxLength(50).IsRequired();
        b.HasIndex(x => x.PlanCode).IsUnique();
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.MonthlyPriceTRY).HasColumnType("numeric(18,2)").IsRequired();
        b.Property(x => x.AnnualPriceTRY).HasColumnType("numeric(18,2)");
        b.Property(x => x.TrialDays);
        b.Property(x => x.BadgeLabel).HasMaxLength(50);
        b.Property(x => x.MaxActiveOffers);
        b.Property(x => x.HasPriorityBoost).IsRequired().HasDefaultValue(false);
        b.Property(x => x.HasFullAnalytics).IsRequired().HasDefaultValue(false);
        b.Property(x => x.SortOrder).IsRequired().HasDefaultValue(0);
        b.Property(x => x.ValidFrom);
        b.Property(x => x.ValidTo);
        b.Property(x => x.FeatureItems)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, _json),
                v => JsonSerializer.Deserialize<List<PlanFeatureItem>>(v, _json) ?? new())
            .HasDefaultValueSql("'[]'::jsonb");
    }
}

public sealed class ProviderPlanSubscriptionConfiguration : IEntityTypeConfiguration<ProviderPlanSubscriptionEntity>
{
    public void Configure(EntityTypeBuilder<ProviderPlanSubscriptionEntity> b)
    {
        b.ToTable("provider_plan_subscriptions");
        b.HasKey(x => x.Id);
        b.Property(x => x.ProviderProfileId).IsRequired();
        b.Property(x => x.ProviderPlanId).IsRequired();
        b.Property(x => x.Status).HasConversion<int>().IsRequired();
        b.Property(x => x.PaidAmount).HasColumnType("numeric(18,2)").IsRequired();
        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        b.Property(x => x.SubscriptionPeriodStart).IsRequired();
        b.Property(x => x.SubscriptionPeriodEnd).IsRequired();
        b.Property(x => x.CommissionRateAtSubscription).HasColumnType("numeric(6,4)").IsRequired();
        b.Property(x => x.PriceChangeReminderVersionKey).HasMaxLength(100);   // N1 idempotency marker (nullable)
        b.HasIndex(x => new { x.ProviderProfileId, x.Status });
    }
}
