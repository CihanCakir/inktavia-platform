using Aizen.Modules.Payment.Domain.Entities.Plan;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Payment.Repository.Persistence.Configurations;

/// <summary>
/// EF mapping for provider plan prices (BE-P4). Money numeric(18,4); enums int; unique filtered PriceCode;
/// resolution indexes on (ProviderPlanId, CurrencyCode, BillingPeriod, Status) and (EffectiveFrom, EffectiveTo).
/// </summary>
public sealed class ProviderPlanPriceConfiguration : IEntityTypeConfiguration<ProviderPlanPriceEntity>
{
    public void Configure(EntityTypeBuilder<ProviderPlanPriceEntity> b)
    {
        b.ToTable("provider_plan_prices");
        b.HasKey(x => x.Id);

        b.Property(x => x.ProviderPlanId).IsRequired();
        b.Property(x => x.PriceType).HasConversion<int>().IsRequired();
        b.Property(x => x.BillingPeriod).HasConversion<int>().IsRequired();
        b.Property(x => x.PriceAmount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        b.Property(x => x.EffectiveFrom).IsRequired();
        b.Property(x => x.EffectiveTo);
        b.Property(x => x.Status).HasConversion<int>().IsRequired();
        b.Property(x => x.PriceCode).HasMaxLength(20);
        b.Property(x => x.Notes).HasMaxLength(1000);

        b.HasIndex(x => x.PriceCode).IsUnique().HasFilter("\"PriceCode\" IS NOT NULL");
        b.HasIndex(x => new { x.ProviderPlanId, x.CurrencyCode, x.BillingPeriod, x.Status });
        b.HasIndex(x => new { x.EffectiveFrom, x.EffectiveTo });
    }
}
