using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Transaction;
using Aizen.Modules.Payment.Repository.Persistence;
using Aizen.Modules.Payment.Repository.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Aizen.Modules.Payment.Repository.UnitTests;

/// <summary>
/// FIX_ESCROW_CAPTURE_AT_INIT — end-to-end over a real PaymentTransactionRepository + DbContext, proving the fix's
/// core claim: because an async-gateway escrow is left PendingIntent (CapturedAt == null) at init, the later payment
/// webhook's <c>if (tx.CapturedAt.HasValue)</c> idempotency guard is FALSE, so the webhook proceeds to Capture AND
/// RecordGatewayItemBreakdown (records the sub-merchant item transaction id needed for release/refund/settlement).
///
/// This replays the exact inline logic of ProcessIyzicoWebhookCommandHandler (GetByGatewayReference → CapturedAt guard →
/// Capture + RecordGatewayItemBreakdown) — the handler is unchanged; only these persistence assertions are added.
/// A duplicate webhook idempotency-skips and never re-records or double-captures.
/// </summary>
public sealed class EscrowWebhookCaptureFixTests
{
    private static PaymentDbContext NewDb()
        => new(new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase($"escrow-webhook-{Guid.NewGuid():N}").Options);

    private static PaymentTransactionEntity NewEscrow() =>
        PaymentTransactionEntity.Create(
            transactionCode: $"TXN-{Guid.NewGuid():N}"[..20], transactionType: TransactionType.ServiceRequestEscrow,
            contextType: TransactionContextType.ServiceRequest, contextId: 1, contextSubId: 1,
            payerProfileId: 100, recipientProfileId: 200, grossAmount: 5974m,
            commissionAmount: 600m, commissionRateSnapshot: 0.12m, vatOnCommission: 120m, netPayoutAmount: 5200m,
            discountAmount: 0m, currencyCode: "TRY", gatewayProvider: "iyzico",
            idempotencyKey: "SR-1-OFFER-1", escrowRequired: true);

    /// <summary>Init branch for an async gateway (CapturedAtInit=false): attach the checkout token, stay PendingIntent.</summary>
    private static async Task<PaymentTransactionEntity> InitEscrowPendingAsync(PaymentDbContext db, string token = "IYZ-TOKEN-1")
    {
        var tx = NewEscrow();
        tx.AttachGatewayReference(token);
        db.Transactions.Add(tx);
        await db.SaveChangesAsync();
        return tx;
    }

    [Fact]
    public async Task AsyncGatewayInit_PersistsPendingIntent_WithReference_AndNullCapturedAt()
    {
        await using var db = NewDb();
        var tx = await InitEscrowPendingAsync(db);

        var repo = new PaymentTransactionRepository(db);
        var reloaded = await repo.GetByGatewayReferenceAsync("IYZ-TOKEN-1", default);

        reloaded.Should().NotBeNull();
        reloaded!.Status.Should().Be(PaymentTransactionStatus.PendingIntent);
        reloaded.CapturedAt.Should().BeNull();
        reloaded.GatewayReference.Should().Be("IYZ-TOKEN-1");
        reloaded.GatewayItemTransactionId.Should().BeNull();   // not recorded until the webhook
    }

    [Fact]
    public async Task Webhook_OnPendingIntent_Captures_AndRecordsItemBreakdown()
    {
        await using var db = NewDb();
        await InitEscrowPendingAsync(db);
        var repo = new PaymentTransactionRepository(db);

        // Replay the webhook's inline capture logic.
        var tx = await repo.GetByGatewayReferenceAsync("IYZ-TOKEN-1", default);
        tx!.CapturedAt.HasValue.Should().BeFalse("the guard must be false so the webhook proceeds — this is the fix");

        tx.Capture("IYZ-TOKEN-1");
        tx.RecordGatewayItemBreakdown("PTX-ITEM-99", subMerchantPayoutAmount: 5200m);
        repo.Update(tx);
        await db.SaveChangesAsync();

        var reloaded = await repo.GetByGatewayReferenceAsync("IYZ-TOKEN-1", default);
        reloaded!.Status.Should().Be(PaymentTransactionStatus.Captured);
        reloaded.CapturedAt.Should().NotBeNull();
        reloaded.GatewayItemTransactionId.Should().Be("PTX-ITEM-99");
        reloaded.SubMerchantPayoutAmount.Should().Be(5200m);
    }

    [Fact]
    public async Task DuplicateWebhook_IdempotencySkips_NoRecapture_NoRebreakdown()
    {
        await using var db = NewDb();
        await InitEscrowPendingAsync(db);
        var repo = new PaymentTransactionRepository(db);

        // First webhook captures + records.
        var tx = await repo.GetByGatewayReferenceAsync("IYZ-TOKEN-1", default);
        tx!.Capture("IYZ-TOKEN-1");
        tx.RecordGatewayItemBreakdown("PTX-ITEM-99", 5200m);
        repo.Update(tx);
        await db.SaveChangesAsync();
        var firstCapturedAt = tx.CapturedAt;

        // Second (duplicate) webhook: the handler's CapturedAt.HasValue guard is now TRUE → it returns without touching the tx.
        var dup = await repo.GetByGatewayReferenceAsync("IYZ-TOKEN-1", default);
        dup!.CapturedAt.HasValue.Should().BeTrue();

        // Guard held: the item breakdown + capture timestamp are unchanged; a forced re-Capture would throw (invalid state).
        dup.GatewayItemTransactionId.Should().Be("PTX-ITEM-99");
        dup.CapturedAt.Should().Be(firstCapturedAt);
        var recapture = () => dup.Capture("IYZ-TOKEN-1");
        recapture.Should().Throw<AizenBusinessException>();
    }
}
