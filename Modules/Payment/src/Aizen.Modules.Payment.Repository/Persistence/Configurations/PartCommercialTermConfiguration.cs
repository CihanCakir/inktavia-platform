using Aizen.Modules.Payment.Domain.Entities.PartCommercialTerm;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Payment.Repository.Persistence.Configurations;

/// <summary>
/// BE-S5a — EF mapping for the versioned/scoped part commercial term. Money numeric(18,4); enums int; effective dates timestamptz
/// (Npgsql default). Unique filtered index on TermCode; composite lookup indexes for scoped effective-date resolution. The cost
/// columns (SupplierListPrice / ProviderDealerMargin) are Payment-internal — no projection outside the module reads them.
/// </summary>
public sealed class PartCommercialTermConfiguration : IEntityTypeConfiguration<PartCommercialTermEntity>
{
    public void Configure(EntityTypeBuilder<PartCommercialTermEntity> b)
    {
        b.ToTable("part_commercial_terms");
        b.HasKey(x => x.Id);

        // ── Scope ──
        b.Property(x => x.Brand).HasMaxLength(100);
        b.Property(x => x.ProductCode).HasMaxLength(100);
        b.Property(x => x.ProviderProfileId);
        b.Property(x => x.CategoryCode).HasMaxLength(100);
        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();

        // ── Confidential cost / margin (Payment-internal) ──
        b.Property(x => x.SupplierListPrice).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.ProviderDealerMargin).HasColumnType("numeric(18,4)").IsRequired();

        // ── Caps + funded split ──
        b.Property(x => x.MaxCustomerDiscount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.SupplierFundedAmount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.ProviderFundedAmount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.PlatformFundedAmount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.MinimumProviderReceivable).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.MaximumDiscountableAmount).HasColumnType("numeric(18,4)").IsRequired();

        // ── Versioning / lifecycle ──
        b.Property(x => x.Version).IsRequired();
        b.Property(x => x.Priority).HasConversion<int>().IsRequired();
        b.Property(x => x.EffectiveFrom).IsRequired();
        b.Property(x => x.EffectiveTo);
        b.Property(x => x.Status).HasConversion<int>().IsRequired();
        b.Property(x => x.TermCode).HasMaxLength(20);
        b.Property(x => x.TermName).HasMaxLength(200);
        b.Property(x => x.Notes).HasMaxLength(1000);

        b.HasIndex(x => x.TermCode).IsUnique().HasFilter("\"TermCode\" IS NOT NULL");
        b.HasIndex(x => new { x.CurrencyCode, x.ProductCode, x.ProviderProfileId, x.Brand, x.CategoryCode, x.Status });
        b.HasIndex(x => new { x.Status, x.EffectiveFrom, x.EffectiveTo });
    }
}
