using Aizen.Modules.Payment.Domain.Entities.Invoice;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Payment.Repository.Persistence.Configurations;

public sealed class InvoiceNumberSequenceConfiguration : IEntityTypeConfiguration<InvoiceNumberSequenceEntity>
{
    public void Configure(EntityTypeBuilder<InvoiceNumberSequenceEntity> b)
    {
        b.ToTable("invoice_number_sequences");
        b.HasKey(x => x.Id);

        b.Property(x => x.Prefix).HasMaxLength(10).IsRequired();
        b.Property(x => x.Year).IsRequired();
        b.Property(x => x.Month).IsRequired();
        b.Property(x => x.LastNumber).IsRequired();
        b.Property(x => x.UpdatedAtUtc).IsRequired();

        // Unique per prefix/year/month — the business key
        b.HasIndex(x => new { x.Prefix, x.Year, x.Month }).IsUnique();
    }
}
