using Aizen.Modules.Payment.Domain.Entities.Transaction;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Payment.Repository.Persistence.Configurations;

public sealed class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransactionEntity>
{
    public void Configure(EntityTypeBuilder<PaymentTransactionEntity> b)
    {
        b.ToTable("transactions");
        b.HasKey(x => x.Id);

        // ── Identity ──────────────────────────────────────────────────────────
        b.Property(x => x.TransactionCode).HasMaxLength(30).IsRequired();
        b.HasIndex(x => x.TransactionCode).IsUnique();

        // ── Context ───────────────────────────────────────────────────────────
        b.Property(x => x.TransactionType).HasConversion<int>().IsRequired();
        b.Property(x => x.ContextType).HasConversion<int>().IsRequired();
        b.Property(x => x.ContextId).IsRequired();
        b.Property(x => x.ContextSubId);

        // ── Parties ───────────────────────────────────────────────────────────
        b.Property(x => x.PayerProfileId).IsRequired();
        b.Property(x => x.RecipientProfileId);

        // ── Amounts ───────────────────────────────────────────────────────────
        b.Property(x => x.GrossAmount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.CommissionAmount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.CommissionRateSnapshot).HasColumnType("numeric(6,4)").IsRequired();
        b.Property(x => x.VatOnCommission).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.NetPayoutAmount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.DiscountAmount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();

        // TotalRefundedAmount: persisted, updated by ApplyRefund/ReverseRefund domain methods
        b.Property(x => x.TotalRefundedAmount)
            .HasColumnType("numeric(18,4)")
            .IsRequired()
            .HasDefaultValue(0m);

        // RemainingRefundableAmount is a computed C# property — not mapped to a column
        b.Ignore(x => x.RemainingRefundableAmount);

        // ── Gateway ───────────────────────────────────────────────────────────
        b.Property(x => x.Status).HasConversion<int>().IsRequired();
        b.Property(x => x.GatewayProvider).HasMaxLength(50).IsRequired();
        b.Property(x => x.GatewayReference).HasMaxLength(500);
        b.Property(x => x.IdempotencyKey).HasMaxLength(200).IsRequired();
        b.HasIndex(x => x.IdempotencyKey).IsUnique();
        b.Property(x => x.EscrowRequired).IsRequired().HasDefaultValue(false);

        // ── Capture / Release lifecycle ───────────────────────────────────────
        b.Property(x => x.CapturedAt);
        b.Property(x => x.ReleasedAt);

        // ── Cancellation lifecycle ────────────────────────────────────────────
        b.Property(x => x.CancelledAt);
        b.Property(x => x.CancellationReason).HasConversion<int?>();
        b.Property(x => x.ReinstatedAt);
        b.Property(x => x.ReinstationNote).HasMaxLength(500);

        // ── Refund lifecycle ──────────────────────────────────────────────────
        b.Property(x => x.LastRefundedAt);

        // ── Dispute lifecycle ─────────────────────────────────────────────────
        b.Property(x => x.DisputedAt);
        b.Property(x => x.DisputeResolution).HasMaxLength(1000);

        // ── Admin ─────────────────────────────────────────────────────────────
        b.Property(x => x.AdminNote).HasMaxLength(500);

        // ── Navigation: refund records ────────────────────────────────────────
        b.HasMany(x => x.RefundRecords)
            .WithOne(r => r.Transaction)
            .HasForeignKey(r => r.PaymentTransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Indexes ───────────────────────────────────────────────────────────
        b.HasIndex(x => new { x.ContextType, x.ContextId });
        b.HasIndex(x => x.PayerProfileId);
        b.HasIndex(x => x.Status);
    }
}
