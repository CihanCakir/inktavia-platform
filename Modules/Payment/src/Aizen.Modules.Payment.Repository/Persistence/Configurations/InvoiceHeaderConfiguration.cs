using Aizen.Modules.Payment.Domain.Entities.Invoice;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Payment.Repository.Persistence.Configurations;

public sealed class InvoiceHeaderConfiguration : IEntityTypeConfiguration<InvoiceHeaderEntity>
{
    public void Configure(EntityTypeBuilder<InvoiceHeaderEntity> b)
    {
        b.ToTable("invoice_headers");
        b.HasKey(x => x.Id);

        // ── Identity ──────────────────────────────────────────────────────────
        b.Property(x => x.InvoiceNumber).HasMaxLength(30);
        b.HasIndex(x => x.InvoiceNumber).IsUnique().HasFilter("\"InvoiceNumber\" IS NOT NULL");

        b.Property(x => x.InvoiceType).HasConversion<int>().IsRequired();
        b.Property(x => x.CommercialModel).HasConversion<int>().IsRequired();
        b.Property(x => x.BillingMode).HasConversion<int>().IsRequired();
        b.Property(x => x.Status).HasConversion<int>().IsRequired();

        // ── Source linkage ────────────────────────────────────────────────────
        b.Property(x => x.SourceType).HasConversion<int>().IsRequired();
        b.Property(x => x.SourceId);

        // ── Payment linkage (cross-module by Id; no EF FK constraints) ───────
        b.Property(x => x.PaymentTransactionId);
        b.Property(x => x.PaymentReleaseId);
        b.Property(x => x.CommissionCalculationId);
        b.Property(x => x.ProviderPayoutId);
        b.Property(x => x.UserSubscriptionId);
        b.Property(x => x.OriginalInvoiceId);

        // ── Seller party snapshot ─────────────────────────────────────────────
        b.Property(x => x.SellerUserId);
        b.Property(x => x.SellerName).HasMaxLength(300).IsRequired();
        b.Property(x => x.SellerTaxNumber).HasMaxLength(20);
        b.Property(x => x.SellerTaxOffice).HasMaxLength(100);
        b.Property(x => x.SellerAddress).HasMaxLength(500);

        // ── Buyer party snapshot ──────────────────────────────────────────────
        b.Property(x => x.BuyerUserId);
        b.Property(x => x.BuyerName).HasMaxLength(300).IsRequired();
        b.Property(x => x.BuyerTaxNumber).HasMaxLength(20);
        b.Property(x => x.BuyerTaxOffice).HasMaxLength(100);
        b.Property(x => x.BuyerAddress).HasMaxLength(500);

        // ── Amounts ───────────────────────────────────────────────────────────
        b.Property(x => x.Currency).HasMaxLength(10).IsRequired();
        b.Property(x => x.SubTotalAmount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.DiscountAmount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.TaxableAmount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.TaxAmount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.TotalAmount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.PaidAmount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.RemainingAmount).HasColumnType("numeric(18,4)").IsRequired();

        // ── Dates ─────────────────────────────────────────────────────────────
        b.Property(x => x.IssueDateUtc);
        b.Property(x => x.DueDateUtc);
        b.Property(x => x.PaidAtUtc);
        b.Property(x => x.CancelledAtUtc);

        // ── External / PDF ────────────────────────────────────────────────────
        b.Property(x => x.ExternalInvoiceId).HasMaxLength(200);
        b.Property(x => x.ExternalInvoiceProvider).HasMaxLength(50);
        b.Property(x => x.PdfFileRef).HasMaxLength(500);
        b.Property(x => x.Notes).HasMaxLength(2000);

        // ── Navigation: Lines ─────────────────────────────────────────────────
        b.HasMany(x => x.Lines)
            .WithOne(l => l.InvoiceHeader)
            .HasForeignKey(l => l.InvoiceHeaderId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Navigation: TaxBreakdowns ─────────────────────────────────────────
        b.HasMany(x => x.TaxBreakdowns)
            .WithOne(t => t.InvoiceHeader)
            .HasForeignKey(t => t.InvoiceHeaderId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Navigation: StatusHistory ─────────────────────────────────────────
        b.HasMany(x => x.StatusHistory)
            .WithOne(h => h.InvoiceHeader)
            .HasForeignKey(h => h.InvoiceHeaderId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Navigation: ExternalIntegration (1:1, optional) ───────────────────
        b.HasOne(x => x.ExternalIntegration)
            .WithOne(e => e.InvoiceHeader)
            .HasForeignKey<InvoiceExternalIntegrationEntity>(e => e.InvoiceHeaderId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Private backing fields ─────────────────────────────────────────────
        b.Navigation(x => x.Lines).HasField("_lines");
        b.Navigation(x => x.TaxBreakdowns).HasField("_taxBreakdowns");
        b.Navigation(x => x.StatusHistory).HasField("_statusHistory");

        // ── Indexes ───────────────────────────────────────────────────────────
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.InvoiceType);
        b.HasIndex(x => x.BuyerUserId);
        // WS1 financial defense-in-depth: partial UNIQUE — at most one auto-issued financial invoice
        // per payment transaction. Filtered to the two consumer-issued types (InvoiceType 2=CommissionInvoice,
        // 3=SubscriptionInvoice) so it cannot false-collide with CreditNote / SalesInvoice / CargoDry /
        // manual drafts that legitimately reference the same PaymentTransactionId.
        b.HasIndex(x => x.PaymentTransactionId)
            .IsUnique()
            .HasFilter("\"PaymentTransactionId\" IS NOT NULL AND \"InvoiceType\" IN (2, 3)");
        b.HasIndex(x => x.OriginalInvoiceId).HasFilter("\"OriginalInvoiceId\" IS NOT NULL");
        b.HasIndex(x => new { x.Status, x.DueDateUtc });   // InvoiceOverdueCheckJob
    }
}
