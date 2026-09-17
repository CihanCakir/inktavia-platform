using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Application.Commands.CompleteCargoDrySettlementPayout;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Interface;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Aizen.Modules.CargoDry.Application.UnitTests;

/// <summary>
/// Settle-on-close: when the payout completes and the settlement moves to Settled, its linked SettlementPending
/// attributions must be settled and SettledKitCount written. The already-Settled early-return also repairs stragglers so
/// a re-run heals existing data (the dead-seam state) without any SQL.
/// </summary>
public sealed class CompleteCargoDrySettlementPayoutSettleOnCloseTests
{
    private const long Sid = 2;
    private static readonly DateTime Now = new(2026, 9, 20, 12, 0, 0, DateTimeKind.Utc);

    private static CargoDrySalesAttributionEntity Attr(long kitId, CargoDrySalesAttributionStatus status)
        => CargoDrySalesAttributionEntity.Create(
            kitId:           kitId,
            serialNumber:    $"SN-{kitId}",
            kitCode:         $"KIT-{kitId}",
            productCode:     "STANDARD-90",
            batchCode:       "B1",
            salesChannel:    Aizen.Modules.CargoDry.Abstraction.Enum.SalesChannel.ConsignmentSellThrough,
            commercialModel: CargoDryCommercialModel.PrincipalSale,
            initialStatus:   status,
            nowUtc:          Now,
            providerProfileId: 10,
            consignmentAgreementId: 5,
            currencyCode:    "TRY");

    private static CargoDrySellThroughSettlementEntity ScheduledSettlement()
    {
        var s = CargoDrySellThroughSettlementEntity.Create(
            "STS-10-TRY-STANDARD-90-202609", consignmentAgreementId: 5, providerProfileId: 10,
            productCode: "STANDARD-90", batchCode: null, currencyCode: "TRY",
            periodStartUtc: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            periodEndUtc:   new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc), nowUtc: Now);
        s.MarkReadyForSettlement(Now);
        s.MarkPaymentPrepared(payoutRecordId: 55, preparedByUserId: 1, preparedAtUtc: Now);
        s.MarkInvoicePrepared(invoiceId: 66, preparedByUserId: 1, preparedAtUtc: Now);
        return s; // Scheduled + PayoutRecordId + InvoiceId
    }

    private static CargoDryPayoutLifecycleResultDto PayoutResult(bool alreadyCompleted = false)
        => new()
        {
            PayoutRecordId = 55, PayoutStatus = PayoutStatus.Completed, ProviderProfileId = 10,
            Amount = 60m, CurrencyCode = "TRY", AlreadyCompleted = alreadyCompleted, Message = "ok",
        };

    private static (CompleteCargoDrySettlementPayoutCommandHandler handler,
                    ICargoDrySettlementPayoutLifecycleService lifecycle,
                    ICargoDrySellThroughSettlementRepository settlements)
        Build(CargoDrySellThroughSettlementEntity settlement, IReadOnlyList<CargoDrySalesAttributionEntity> linked)
    {
        var settlements = Substitute.For<ICargoDrySellThroughSettlementRepository>();
        settlements.GetByIdAsync(Sid, Arg.Any<CancellationToken>()).Returns(settlement);

        var attributions = Substitute.For<ICargoDrySalesAttributionRepository>();
        attributions.GetBySettlementIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(linked);

        var lifecycle = Substitute.For<ICargoDrySettlementPayoutLifecycleService>();
        lifecycle.CompleteManualAsync(Arg.Any<long>(), Arg.Any<long>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(PayoutResult());
        lifecycle.GetPayoutStateAsync(Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(PayoutResult(alreadyCompleted: true));

        var handler = new CompleteCargoDrySettlementPayoutCommandHandler(
            settlements, attributions, lifecycle, NullLogger<CompleteCargoDrySettlementPayoutCommandHandler>.Instance);
        return (handler, lifecycle, settlements);
    }

    private static CompleteCargoDrySettlementPayoutCommand Cmd() => new()
    {
        SettlementId = Sid, CompletedByUserId = 1, ManualPaymentReference = "EFT-1",
    };

    [Fact]
    public async Task Complete_settles_pending_attributions_and_records_count()
    {
        var settlement = ScheduledSettlement();
        var a1 = Attr(1, CargoDrySalesAttributionStatus.SettlementPending);
        var a2 = Attr(2, CargoDrySalesAttributionStatus.SettlementPending);
        var (handler, _, settlements) = Build(settlement, new[] { a1, a2 });

        var resp = await handler.Handle(Cmd(), default);

        settlement.Status.Should().Be(CargoDrySellThroughSettlementStatus.Settled);
        a1.Status.Should().Be(CargoDrySalesAttributionStatus.Settled);
        a2.Status.Should().Be(CargoDrySalesAttributionStatus.Settled);
        settlement.SettledKitCount.Should().Be(2);
        resp.Settlement.SettledKitCount.Should().Be(2);
        resp.AlreadyCompleted.Should().BeFalse();
        await settlements.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cancelled_and_other_non_settleable_attributions_are_skipped_without_failing()
    {
        var settlement = ScheduledSettlement();
        var pending   = Attr(1, CargoDrySalesAttributionStatus.SettlementPending);
        var cancelled = Attr(2, CargoDrySalesAttributionStatus.Cancelled);
        var review    = Attr(3, CargoDrySalesAttributionStatus.CommercialReviewRequired); // stands in for "disputed/other"
        var (handler, _, _) = Build(settlement, new[] { pending, cancelled, review });

        var act = () => handler.Handle(Cmd(), default);
        await act.Should().NotThrowAsync("a straggler must never fail the whole complete-payout command");

        pending.Status.Should().Be(CargoDrySalesAttributionStatus.Settled);
        cancelled.Status.Should().Be(CargoDrySalesAttributionStatus.Cancelled, "Cancelled is skipped, not settled");
        review.Status.Should().Be(CargoDrySalesAttributionStatus.CommercialReviewRequired, "non-settleable is skipped");
        settlement.SettledKitCount.Should().Be(1, "only the settled attribution counts");
    }

    [Fact]
    public async Task Already_settled_settlement_repairs_pending_stragglers_and_still_reports_already_completed()
    {
        var settlement = ScheduledSettlement();
        settlement.MarkPayoutCompleted(1, Now, "EFT-0"); // already Settled, SettledKitCount still 0 (the dead-seam state)
        settlement.SettledKitCount.Should().Be(0);

        var a1 = Attr(1, CargoDrySalesAttributionStatus.SettlementPending);
        var a2 = Attr(2, CargoDrySalesAttributionStatus.SettlementPending);
        var (handler, _, settlements) = Build(settlement, new[] { a1, a2 });

        var resp = await handler.Handle(Cmd(), default);

        resp.AlreadyCompleted.Should().BeTrue();
        a1.Status.Should().Be(CargoDrySalesAttributionStatus.Settled, "the repair path settles stragglers");
        a2.Status.Should().Be(CargoDrySalesAttributionStatus.Settled);
        settlement.SettledKitCount.Should().Be(2);
        resp.Settlement.SettledKitCount.Should().Be(2);
        await settlements.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void RecordSettledKits_rejects_non_settled_status()
    {
        var scheduled = ScheduledSettlement(); // Scheduled, not Settled
        scheduled.Invoking(s => s.RecordSettledKits(1))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*must be Settled*");
    }
}
