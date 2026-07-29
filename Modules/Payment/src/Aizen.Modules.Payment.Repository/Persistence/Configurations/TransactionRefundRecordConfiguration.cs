using Aizen.Modules.Payment.Domain.Entities.Transaction;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Payment.Repository.Persistence.Configurations;

public sealed class TransactionRefundRecordConfiguration : IEntityTypeConfiguration<TransactionRefundRecord>
{
    public void Configure(EntityTypeBuilder<TransactionRefundRecord> b)
    {
        b.ToTable("transaction_refund_records");
        b.HasKey(x => x.Id);

        // ── FK ────────────────────────────────────────────────────────────────
        b.Property(x => x.PaymentTransactionId).IsRequired();
        b.HasOne(x => x.Transaction)
            .WithMany(t => t.RefundRecords)
            .HasForeignKey(x => x.PaymentTransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Identity ──────────────────────────────────────────────────────────
        b.Property(x => x.RefundCode).HasMaxLength(30).IsRequired();
        b.HasIndex(x => x.RefundCode).IsUnique();

        // ── Classification ────────────────────────────────────────────────────
        b.Property(x => x.RefundType).HasConversion<int>().IsRequired();
        b.Property(x => x.Reason).HasConversion<int>().IsRequired();
        b.Property(x => x.Status).HasConversion<int>().IsRequired();

        // ── BE-P10: allocation link + cause/release-state + benefit-restore-once guard ──
        b.Property(x => x.Cause).HasConversion<int>();
        b.Property(x => x.ReleaseState).HasConversion<int>();
        b.Property(x => x.RefundAllocationId);
        b.Property(x => x.BenefitRestoreApplied).IsRequired().HasDefaultValue(false);

        // ── Amount ────────────────────────────────────────────────────────────
        b.Property(x => x.Amount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();

        // ── Gateway ───────────────────────────────────────────────────────────
        b.Property(x => x.GatewayRefundReference).HasMaxLength(500);

        // ── Processing ────────────────────────────────────────────────────────
        b.Property(x => x.ProcessedAt);
        b.Property(x => x.FailureReason).HasMaxLength(1000);
        b.Property(x => x.AdminNote).HasMaxLength(500);

        // ── Reversal ──────────────────────────────────────────────────────────
        b.Property(x => x.ReversedAt);
        b.Property(x => x.ReversalReason).HasMaxLength(500);
        b.Property(x => x.ReversalAdminNote).HasMaxLength(500);

        // ── Indexes ───────────────────────────────────────────────────────────
        b.HasIndex(x => x.PaymentTransactionId);
        b.HasIndex(x => x.Status);
    }
}
