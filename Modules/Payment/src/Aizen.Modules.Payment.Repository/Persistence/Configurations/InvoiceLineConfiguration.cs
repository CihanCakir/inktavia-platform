using Aizen.Modules.Payment.Domain.Entities.Invoice;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Payment.Repository.Persistence.Configurations;

public sealed class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLineEntity>
{
    public void Configure(EntityTypeBuilder<InvoiceLineEntity> b)
    {
        b.ToTable("invoice_lines");
        b.HasKey(x => x.Id);

        b.Property(x => x.InvoiceHeaderId).IsRequired();
        b.Property(x => x.LineNumber).IsRequired();
        b.Property(x => x.LineType).HasConversion<int>().IsRequired();
        b.Property(x => x.Description).HasMaxLength(500).IsRequired();
        b.Property(x => x.ProductCode).HasMaxLength(50);
        b.Property(x => x.ServiceCategoryCode).HasMaxLength(100);

        // ── Quantities ────────────────────────────────────────────────────────
        b.Property(x => x.Quantity).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.UnitCode).HasMaxLength(10).IsRequired();

        // ── Amounts ───────────────────────────────────────────────────────────
        b.Property(x => x.UnitPrice).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.LineSubTotal).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.DiscountAmount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.TaxRate).HasColumnType("numeric(6,4)").IsRequired();
        b.Property(x => x.TaxAmount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.LineTotal).HasColumnType("numeric(18,4)").IsRequired();

        // ── Source traceability ───────────────────────────────────────────────
        b.Property(x => x.SourceType).HasConversion<int>();
        b.Property(x => x.SourceId);

        // FK declared in InvoiceHeaderConfiguration (HasMany → WithOne)
        b.HasIndex(x => x.InvoiceHeaderId);
        b.HasIndex(x => new { x.InvoiceHeaderId, x.LineNumber }).IsUnique();
    }
}
