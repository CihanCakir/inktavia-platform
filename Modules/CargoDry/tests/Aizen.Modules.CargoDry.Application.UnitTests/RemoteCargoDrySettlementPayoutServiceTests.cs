using Aizen.Modules.CargoDry.Application.Services;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.RemoteCall;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Aizen.Modules.CargoDry.Application.UnitTests;

/// <summary>
/// The split-host HTTP bridge that replaces the in-process Payment.Application MediatR call. It forwards the settlement
/// details to Payment's cluster-internal endpoint and maps the response back to the seam's result type — keeping the
/// flow synchronous so the calling handler can MarkPaymentPrepared with a real PayoutRecordId. Idempotency is delegated
/// to the Payment side (settlement-id unique) and surfaced verbatim via AlreadyExisted.
/// </summary>
public sealed class RemoteCargoDrySettlementPayoutServiceTests
{
    private static (RemoteCargoDrySettlementPayoutService svc, ICargoDrySettlementPaymentRemoteCall remote) Build()
    {
        var remote = Substitute.For<ICargoDrySettlementPaymentRemoteCall>();
        var svc = new RemoteCargoDrySettlementPayoutService(
            remote, NullLogger<RemoteCargoDrySettlementPayoutService>.Instance);
        return (svc, remote);
    }

    [Fact]
    public async Task Forwards_settlement_details_and_maps_the_response()
    {
        var (svc, remote) = Build();
        remote.PrepareSettlementPayoutAsync(Arg.Any<PrepareCargoDrySettlementPayoutRemoteCallRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PrepareCargoDrySettlementPayoutRemoteCallResponse
            {
                PayoutRecordId = 4242, Status = PayoutStatus.Pending, AlreadyExisted = false,
            });

        var result = await svc.PrepareSettlementPayoutAsync(
            settlementId: 2, settlementCode: "STS-10-TRY-STANDARD-90-202609", providerProfileId: 10,
            amount: 60.00m, currencyCode: "TRY", description: "payout", preparedByUserId: 7);

        result.PayoutRecordId.Should().Be(4242);
        result.Status.Should().Be(PayoutStatus.Pending);
        result.AlreadyExisted.Should().BeFalse();

        await remote.Received(1).PrepareSettlementPayoutAsync(
            Arg.Is<PrepareCargoDrySettlementPayoutRemoteCallRequest>(r =>
                r.SourceSettlementId == 2 &&
                r.SettlementCode == "STS-10-TRY-STANDARD-90-202609" &&
                r.ProviderProfileId == 10 &&
                r.Amount == 60.00m &&
                r.CurrencyCode == "TRY" &&
                r.PreparedByUserId == 7),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Re_prepare_passes_through_the_idempotency_flag()
    {
        var (svc, remote) = Build();
        remote.PrepareSettlementPayoutAsync(Arg.Any<PrepareCargoDrySettlementPayoutRemoteCallRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PrepareCargoDrySettlementPayoutRemoteCallResponse
            {
                PayoutRecordId = 999, Status = PayoutStatus.Pending, AlreadyExisted = true,
            });

        var result = await svc.PrepareSettlementPayoutAsync(
            2, "STS-1", 10, 60m, "TRY", "payout", 7);

        result.PayoutRecordId.Should().Be(999);
        result.AlreadyExisted.Should().BeTrue("a re-prepare must surface the existing payout, not a duplicate");
    }

    [Fact]
    public async Task Throws_when_the_remote_returns_no_result()
    {
        var (svc, remote) = Build();
        remote.PrepareSettlementPayoutAsync(Arg.Any<PrepareCargoDrySettlementPayoutRemoteCallRequest>(), Arg.Any<CancellationToken>())
            .Returns((PrepareCargoDrySettlementPayoutRemoteCallResponse)null!);

        await svc.Invoking(s => s.PrepareSettlementPayoutAsync(2, "STS-1", 10, 60m, "TRY", "payout", 7))
            .Should().ThrowAsync<InvalidOperationException>();
    }
}
