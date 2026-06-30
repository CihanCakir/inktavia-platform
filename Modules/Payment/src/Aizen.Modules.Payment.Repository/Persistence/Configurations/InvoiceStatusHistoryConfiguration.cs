using Aizen.Modules.Payment.Domain.Entities.Invoice;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Payment.Repository.Persistence.Configurations;

public sealed class InvoiceStatusHistoryConfiguration : IEntityTypeConfiguration<InvoiceStatusHistoryEntity>
{
    public void Configure(EntityTypeBuilder<InvoiceStatusHistoryEntity> b)
    {
        b.ToTable("invoice_status_history");
        b.HasKey(x => x.Id);

        b.Property(x => x.InvoiceHeaderId).IsRequired();
        b.Property(x => x.OldStatus).HasConversion<int>().IsRequired();
        b.Property(x => x.NewStatus).HasConversion<int>().IsRequired();
        b.Property(x => x.Reason).HasMaxLength(500);
        b.Property(x => x.ChangedByUserId);
        b.Property(x => x.ChangedAtUtc).IsRequired();

        // FK declared in InvoiceHeaderConfiguration (HasMany → WithOne)
        b.HasIndex(x => x.InvoiceHeaderId);
        b.HasIndex(x => x.ChangedAtUtc);
    }
}
