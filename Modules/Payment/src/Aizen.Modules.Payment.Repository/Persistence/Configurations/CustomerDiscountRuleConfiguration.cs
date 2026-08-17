using Aizen.Modules.Payment.Domain.Entities.CustomerDiscount;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Payment.Repository.Persistence.Configurations;

/// <summary>EF mapping for customer discount rules (BE-P6). Money numeric(18,4); rates numeric(9,4); enums int.</summary>
public sealed class CustomerDiscountRuleConfiguration : IEntityTypeConfiguration<CustomerDiscountRuleEntity>
{
    public void Configure(EntityTypeBuilder<CustomerDiscountRuleEntity> b)
    {
        b.ToTable("customer_discount_rules");
        b.HasKey(x => x.Id);

        b.Property(x => x.CustomerPlanId);
        b.Property(x => x.CategoryCode).HasMaxLength(100);
        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();

        b.Property(x => x.DiscountType).HasConversion<int>().IsRequired();
        b.Property(x => x.DiscountRate).HasColumnType("numeric(9,4)");
        b.Property(x => x.FixedDiscountAmount).HasColumnType("numeric(18,4)");
        b.Property(x => x.MinimumPurchaseAmount).HasColumnType("numeric(18,4)");
        b.Property(x => x.MaximumDiscountAmount).HasColumnType("numeric(18,4)");

        b.Property(x => x.FundingMode).HasConversion<int>().IsRequired();
        b.Property(x => x.PlatformFundingRate).HasColumnType("numeric(9,4)");
        b.Property(x => x.ProviderFundingRate).HasColumnType("numeric(9,4)");
        b.Property(x => x.RequiresProviderConsent).IsRequired();

        b.Property(x => x.Priority).HasConversion<int>().IsRequired();
        b.Property(x => x.EffectiveFrom).IsRequired();
        b.Property(x => x.EffectiveTo);
        b.Property(x => x.Status).HasConversion<int>().IsRequired();
        b.Property(x => x.RuleCode).HasMaxLength(20);
        b.Property(x => x.RuleName).HasMaxLength(200);
        b.Property(x => x.Notes).HasMaxLength(1000);

        b.HasIndex(x => x.RuleCode).IsUnique().HasFilter("\"RuleCode\" IS NOT NULL");
        b.HasIndex(x => new { x.CurrencyCode, x.CustomerPlanId, x.CategoryCode, x.Status });
        b.HasIndex(x => new { x.Status, x.EffectiveFrom, x.EffectiveTo });
    }
}
