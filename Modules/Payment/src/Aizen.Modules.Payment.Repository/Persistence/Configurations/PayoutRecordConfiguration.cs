using Aizen.Modules.Payment.Domain.Entities.Payout;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Payment.Repository.Persistence.Configurations;

public sealed class PayoutRecordConfiguration : IEntityTypeConfiguration<PayoutRecordEntity>
{
    public void Configure(EntityTypeBuilder<PayoutRecordEntity> b)
    {
        b.ToTable("payout_records");
        b.HasKey(x => x.Id);
        b.Property(x => x.ProviderProfileId).IsRequired();
        // Phase 4B: nullable — CargoDry settlement payouts have no upstream PaymentTransaction
        b.Property(x => x.PaymentTransactionId);
        // Phase 4B: source fields for cross-module payout origins
        b.Property(x => x.SourceType).HasMaxLength(100);
        b.Property(x => x.SourceId);
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.Amount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        b.Property(x => x.Status).HasConversion<int>().IsRequired();
        b.Property(x => x.GatewayProvider).HasMaxLength(50).IsRequired();
        b.Property(x => x.GatewayPayoutId).HasMaxLength(200);
        b.Property(x => x.RequestedAt).IsRequired();
        b.Property(x => x.FailureReason).HasMaxLength(500);
        b.Property(x => x.AdminNote).HasMaxLength(500);
        // ── Phase 4D: Lifecycle audit fields ──────────────────────────────────────
        b.Property(x => x.ApprovedAtUtc);
        b.Property(x => x.ApprovedByUserId);
        b.Property(x => x.ProcessingAtUtc);
        b.Property(x => x.ProcessingByUserId);
        b.Property(x => x.CompletedByUserId);
        b.Property(x => x.FailedAtUtc);
        b.Property(x => x.FailedByUserId);
        // ── Phase L12: Payout receipt PDF ────────────────────────────────────────
        b.Property(x => x.ReceiptFileRef).HasMaxLength(500);

        b.HasIndex(x => x.ProviderProfileId);
        b.HasIndex(x => x.Status);
        // Composite index for idempotency lookup by source
        b.HasIndex(x => new { x.SourceType, x.SourceId })
         .HasDatabaseName("IX_payout_records_SourceType_SourceId");

        // WS1 financial defense-in-depth: partial UNIQUE — at most one non-Failed payout per released
        // transaction (escrow-release payouts have a non-null PaymentTransactionId; CargoDry settlement
        // payouts are NULL and excluded). Status<>4 (Failed) excludes rejected attempts so a retry after a
        // gateway failure can re-claim the key. Backstops the money-after-persist reorder against the
        // two-phase double-commit.
        b.HasIndex(x => x.PaymentTransactionId)
         .IsUnique()
         .HasFilter("\"PaymentTransactionId\" IS NOT NULL AND \"Status\" <> 4")
         .HasDatabaseName("UX_payout_records_PaymentTransactionId_Active");
    }
}
