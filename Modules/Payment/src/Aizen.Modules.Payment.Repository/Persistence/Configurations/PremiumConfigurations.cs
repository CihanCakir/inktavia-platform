using Aizen.Modules.Payment.Domain.Entities.Premium;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Payment.Repository.Persistence.Configurations;

/// <summary>BE-P11 §9.1 — premium product catalogue. Unique Code.</summary>
public sealed class PremiumProductConfiguration : IEntityTypeConfiguration<PremiumProductEntity>
{
    public void Configure(EntityTypeBuilder<PremiumProductEntity> b)
    {
        b.ToTable("premium_products");
        b.HasKey(x => x.Id);

        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.EntitlementType).HasConversion<int>().IsRequired();
        b.Property(x => x.DurationDays).IsRequired();
        b.Property(x => x.Status).HasConversion<int>().IsRequired();
        b.Property(x => x.Description).HasMaxLength(1000);

        b.HasIndex(x => x.Code).IsUnique();
    }
}

/// <summary>BE-P11 §9.1/§13.9 — versioned premium price (BE-P4 mirror). Unique filtered PriceCode + range index.</summary>
public sealed class PremiumProductPriceConfiguration : IEntityTypeConfiguration<PremiumProductPriceEntity>
{
    public void Configure(EntityTypeBuilder<PremiumProductPriceEntity> b)
    {
        b.ToTable("premium_product_prices");
        b.HasKey(x => x.Id);

        b.Property(x => x.PremiumProductId).IsRequired();
        b.Property(x => x.PriceAmount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        b.Property(x => x.EffectiveFrom).IsRequired();
        b.Property(x => x.EffectiveTo);
        b.Property(x => x.Status).HasConversion<int>().IsRequired();
        b.Property(x => x.PriceCode).HasMaxLength(30);
        b.Property(x => x.Notes).HasMaxLength(1000);

        b.HasIndex(x => x.PriceCode).IsUnique().HasFilter("\"PriceCode\" IS NOT NULL");
        b.HasIndex(x => new { x.PremiumProductId, x.CurrencyCode, x.Status });
        b.HasIndex(x => new { x.EffectiveFrom, x.EffectiveTo });
    }
}

/// <summary>BE-P11 §9.2 — premium purchases. Unique PurchaseCode; snapshot money numeric(18,4).</summary>
public sealed class PremiumPurchaseConfiguration : IEntityTypeConfiguration<PremiumPurchaseEntity>
{
    public void Configure(EntityTypeBuilder<PremiumPurchaseEntity> b)
    {
        b.ToTable("premium_purchases");
        b.HasKey(x => x.Id);

        b.Property(x => x.ProviderProfileId).IsRequired();
        b.Property(x => x.PremiumProductId).IsRequired();
        b.Property(x => x.ProductCodeSnapshot).HasMaxLength(50).IsRequired();
        b.Property(x => x.PremiumProductPriceIdSnapshot).IsRequired();
        b.Property(x => x.UnitPriceSnapshot).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.CurrencyCodeSnapshot).HasMaxLength(10).IsRequired();
        b.Property(x => x.DurationDaysSnapshot).IsRequired();
        b.Property(x => x.ContextRef).IsRequired();
        b.Property(x => x.PaymentTransactionId);
        b.Property(x => x.Status).HasConversion<int>().IsRequired();
        b.Property(x => x.PurchaseCode).HasMaxLength(40).IsRequired();

        b.HasIndex(x => x.PurchaseCode).IsUnique();
        b.HasIndex(x => x.PaymentTransactionId);
        b.HasIndex(x => new { x.ProviderProfileId, x.ContextRef, x.Status });
    }
}

/// <summary>BE-P11 §9.2 — premium entitlements. <b>Unique PremiumPurchaseId</b> (≤1 per purchase).</summary>
public sealed class PremiumEntitlementConfiguration : IEntityTypeConfiguration<PremiumEntitlementEntity>
{
    public void Configure(EntityTypeBuilder<PremiumEntitlementEntity> b)
    {
        b.ToTable("premium_entitlements");
        b.HasKey(x => x.Id);

        b.Property(x => x.PremiumPurchaseId).IsRequired();
        b.Property(x => x.ProviderProfileId).IsRequired();
        b.Property(x => x.ProductCodeSnapshot).HasMaxLength(50).IsRequired();
        b.Property(x => x.ContextRef).IsRequired();
        b.Property(x => x.Status).HasConversion<int>().IsRequired();
        b.Property(x => x.StartsAt);
        b.Property(x => x.ExpiresAt);
        b.Property(x => x.RevokedAt);
        b.Property(x => x.RevocationReason).HasMaxLength(500);

        b.HasIndex(x => x.PremiumPurchaseId).IsUnique();               // §9.2 — max one entitlement per purchase
        b.HasIndex(x => new { x.ContextRef, x.Status });              // read model GetActiveBoostForOffer
        b.HasIndex(x => new { x.Status, x.ExpiresAt });               // expiration job
    }
}
