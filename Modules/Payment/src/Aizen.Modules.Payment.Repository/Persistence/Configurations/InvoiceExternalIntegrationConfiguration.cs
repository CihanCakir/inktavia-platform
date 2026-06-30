using Aizen.Modules.Payment.Domain.Entities.Invoice;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Payment.Repository.Persistence.Configurations;

public sealed class InvoiceExternalIntegrationConfiguration : IEntityTypeConfiguration<InvoiceExternalIntegrationEntity>
{
    public void Configure(EntityTypeBuilder<InvoiceExternalIntegrationEntity> b)
    {
        b.ToTable("invoice_external_integrations");
        b.HasKey(x => x.Id);

        // FK declared in InvoiceHeaderConfiguration (HasOne → WithOne)
        b.Property(x => x.InvoiceHeaderId).IsRequired();

        b.Property(x => x.Provider).HasMaxLength(50).IsRequired();
        b.Property(x => x.ExternalInvoiceId).HasMaxLength(200);
        b.Property(x => x.Status).HasMaxLength(20).IsRequired();
        b.Property(x => x.ErrorMessage).HasMaxLength(2000);

        // Payload columns: large text, no length cap (UBL-TR XML can exceed 4 KB)
        b.Property(x => x.RawRequestPayload).HasColumnType("text");
        b.Property(x => x.RawResponsePayload).HasColumnType("text");

        b.Property(x => x.CreatedAtUtc).IsRequired();
        b.Property(x => x.UpdatedAtUtc);

        b.HasIndex(x => x.InvoiceHeaderId).IsUnique();
        b.HasIndex(x => x.Status);
    }
}
