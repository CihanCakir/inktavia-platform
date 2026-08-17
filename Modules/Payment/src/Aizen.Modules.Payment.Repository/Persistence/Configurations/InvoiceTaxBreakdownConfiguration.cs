using Aizen.Modules.Payment.Domain.Entities.Invoice;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Payment.Repository.Persistence.Configurations;

public sealed class InvoiceTaxBreakdownConfiguration : IEntityTypeConfiguration<InvoiceTaxBreakdownEntity>
{
    public void Configure(EntityTypeBuilder<InvoiceTaxBreakdownEntity> b)
    {
        b.ToTable("invoice_tax_breakdowns");
        b.HasKey(x => x.Id);

        b.Property(x => x.InvoiceHeaderId).IsRequired();
        b.Property(x => x.TaxType).HasMaxLength(20).IsRequired();
        b.Property(x => x.TaxRate).HasColumnType("numeric(6,4)").IsRequired();
        b.Property(x => x.TaxableAmount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.TaxAmount).HasColumnType("numeric(18,4)").IsRequired();

        // FK declared in InvoiceHeaderConfiguration (HasMany → WithOne)
        b.HasIndex(x => x.InvoiceHeaderId);
        b.HasIndex(x => new { x.InvoiceHeaderId, x.TaxType });
    }
}
