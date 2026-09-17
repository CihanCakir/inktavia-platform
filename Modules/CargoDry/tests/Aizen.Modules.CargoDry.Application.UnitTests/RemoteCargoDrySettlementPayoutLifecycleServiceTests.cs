using Aizen.Modules.CargoDry.Application.Services;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Abstraction.RemoteCall;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Aizen.Modules.CargoDry.Application.UnitTests;

/// <summary>
/// Split-host HTTP bridge for the payout lifecycle. Each method forwards to the Payment cluster-internal endpoint and
/// returns the lifecycle DTO verbatim, keeping the seam synchronous so the CargoDry handlers can act on the result
/// (e.g. Complete → the handler then advances the settlement to Settled itself — this bridge never does).
/// </summary>
public sealed class RemoteCargoDrySettlementPayoutLifecycleServiceTests
{
    private static (RemoteCargoDrySettlementPayoutLifecycleService svc, ICargoDrySettlementPaymentRemoteCall remote) Build()
    {
        var remote = Substitute.For<ICargoDrySettlementPaymentRemoteCall>();
        var svc = new RemoteCargoDrySettlementPayoutLifecycleService(
            remote, NullLogger<RemoteCargoDrySettlementPayoutLifecycleService>.Instance);
        return (svc, remote);
    }

    private static CargoDryPayoutLifecycleResultDto Result(PayoutStatus status, bool alreadyCompleted = false)
        => new()
        {
            PayoutRecordId = 55, PayoutStatus = status, ProviderProfileId = 10, Amount = 60m,
            CurrencyCode = "TRY", AlreadyCompleted = alreadyCompleted, Message = "ok",
        };

    [Fact]
    public async Task GetState_forwards_settlement_id_and_returns_dto()
    {
        var (svc, remote) = Build();
        remote.GetPayoutStateAsync(55, Arg.Any<GetCargoDrySettlementPayoutStateRemoteCallRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result(PayoutStatus.Pending));

        var r = await svc.GetPayoutStateAsync(55, sourceSettlementId: 2);

        r.PayoutRecordId.Should().Be(55);
        await remote.Received(1).GetPayoutStateAsync(55,
            Arg.Is<GetCargoDrySettlementPayoutStateRemoteCallRequest>(x => x.SourceSettlementId == 2), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Approve_forwards_actor_and_note()
    {
        var (svc, remote) = Build();
        remote.ApprovePayoutAsync(55, Arg.Any<ApproveCargoDrySettlementPayoutRemoteCallRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result(PayoutStatus.Approved));

        var r = await svc.ApproveAsync(55, approvedByUserId: 7, note: "ok");

        r.PayoutStatus.Should().Be(PayoutStatus.Approved);
        await remote.Received(1).ApprovePayoutAsync(55,
            Arg.Is<ApproveCargoDrySettlementPayoutRemoteCallRequest>(x => x.ApprovedByUserId == 7 && x.Note == "ok"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkProcessing_forwards_reference()
    {
        var (svc, remote) = Build();
        remote.MarkPayoutProcessingAsync(55, Arg.Any<MarkProcessingCargoDrySettlementPayoutRemoteCallRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result(PayoutStatus.Processing));

        var r = await svc.MarkProcessingAsync(55, processedByUserId: 7, externalReference: "BANK-1", note: null);

        r.PayoutStatus.Should().Be(PayoutStatus.Processing);
        await remote.Received(1).MarkPayoutProcessingAsync(55,
            Arg.Is<MarkProcessingCargoDrySettlementPayoutRemoteCallRequest>(x => x.ProcessedByUserId == 7 && x.ExternalReference == "BANK-1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Complete_passes_through_idempotency_flag()
    {
        var (svc, remote) = Build();
        remote.CompletePayoutManualAsync(55, Arg.Any<CompleteCargoDrySettlementPayoutRemoteCallRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result(PayoutStatus.Completed, alreadyCompleted: true));

        var r = await svc.CompleteManualAsync(55, completedByUserId: 7, manualPaymentReference: "EFT-9", note: null);

        r.PayoutStatus.Should().Be(PayoutStatus.Completed);
        r.AlreadyCompleted.Should().BeTrue("a re-complete must surface the already-completed payout, not duplicate it");
        await remote.Received(1).CompletePayoutManualAsync(55,
            Arg.Is<CompleteCargoDrySettlementPayoutRemoteCallRequest>(x => x.CompletedByUserId == 7 && x.ManualPaymentReference == "EFT-9"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Fail_forwards_reason()
    {
        var (svc, remote) = Build();
        remote.FailPayoutAsync(55, Arg.Any<FailCargoDrySettlementPayoutRemoteCallRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result(PayoutStatus.Failed));

        var r = await svc.FailAsync(55, failedByUserId: 7, failureReason: "bank rejected", externalReference: null, note: null);

        r.PayoutStatus.Should().Be(PayoutStatus.Failed);
        await remote.Received(1).FailPayoutAsync(55,
            Arg.Is<FailCargoDrySettlementPayoutRemoteCallRequest>(x => x.FailureReason == "bank rejected"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_when_remote_returns_null()
    {
        var (svc, remote) = Build();
        remote.ApprovePayoutAsync(55, Arg.Any<ApproveCargoDrySettlementPayoutRemoteCallRequest>(), Arg.Any<CancellationToken>())
            .Returns((CargoDryPayoutLifecycleResultDto)null!);

        await svc.Invoking(s => s.ApproveAsync(55, 7, null)).Should().ThrowAsync<InvalidOperationException>();
    }
}
