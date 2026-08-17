using Aizen.Modules.ServiceRequest.Domain.Entities.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.ServiceRequest.Repository.Persistence.Configurations;

/// <summary>S2a — admin-owned pricing attribute definition. Unique stable Code; category scope via an owned join.</summary>
public sealed class PricingAttributeDefinitionConfiguration : IEntityTypeConfiguration<PricingAttributeDefinitionEntity>
{
    public void Configure(EntityTypeBuilder<PricingAttributeDefinitionEntity> b)
    {
        b.ToTable("pricing_attribute_definitions");
        b.HasKey(x => x.Id);

        b.Property(x => x.Code).IsRequired().HasMaxLength(100);
        b.Property(x => x.NameTr).IsRequired().HasMaxLength(200);
        b.Property(x => x.NameEn).IsRequired().HasMaxLength(200);
        b.Property(x => x.DataType).HasConversion<int>().IsRequired();
        b.Property(x => x.LookupGroupCode).HasMaxLength(100);
        b.Property(x => x.IsRequired).IsRequired();
        b.Property(x => x.SortOrder).IsRequired();
        b.Property(x => x.MinValue).HasPrecision(18, 4);
        b.Property(x => x.MaxValue).HasPrecision(18, 4);

        b.HasIndex(x => x.Code).IsUnique();

        b.HasMany(x => x.Categories)
            .WithOne()
            .HasForeignKey(c => c.PricingAttributeDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Metadata.FindNavigation(nameof(PricingAttributeDefinitionEntity.Categories))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

/// <summary>S2a — applicability scope join (definition × ServiceCategoryCode).</summary>
public sealed class PricingAttributeDefinitionCategoryConfiguration : IEntityTypeConfiguration<PricingAttributeDefinitionCategoryEntity>
{
    public void Configure(EntityTypeBuilder<PricingAttributeDefinitionCategoryEntity> b)
    {
        b.ToTable("pricing_attribute_definition_categories");
        b.HasKey(x => x.Id);

        b.Property(x => x.PricingAttributeDefinitionId).IsRequired();
        b.Property(x => x.ServiceCategoryCode).IsRequired().HasMaxLength(100);

        b.HasIndex(x => x.ServiceCategoryCode);
        b.HasIndex(x => new { x.PricingAttributeDefinitionId, x.ServiceCategoryCode }).IsUnique();
    }
}

/// <summary>S2b — per-offer-line pricing attribute value. At most one value per (line, definition).</summary>
public sealed class PricingAttributeValueConfiguration : IEntityTypeConfiguration<PricingAttributeValueEntity>
{
    public void Configure(EntityTypeBuilder<PricingAttributeValueEntity> b)
    {
        b.ToTable("pricing_attribute_values");
        b.HasKey(x => x.Id);

        b.Property(x => x.OfferItemId).IsRequired();
        b.Property(x => x.DefinitionCode).IsRequired().HasMaxLength(100);
        b.Property(x => x.ValueLookupItemCode).HasMaxLength(100);
        b.Property(x => x.ValueNumber).HasPrecision(18, 4);
        b.Property(x => x.ValueText).HasMaxLength(1000);

        b.HasIndex(x => x.OfferItemId);
        b.HasIndex(x => new { x.OfferItemId, x.DefinitionCode }).IsUnique();
    }
}
