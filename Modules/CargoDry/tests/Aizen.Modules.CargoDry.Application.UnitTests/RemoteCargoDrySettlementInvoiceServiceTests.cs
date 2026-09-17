using Aizen.Modules.CargoDry.Application.Services;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Abstraction.RemoteCall;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Aizen.Modules.CargoDry.Application.UnitTests;

/// <summary>Split-host HTTP bridge for the settlement statement (invoice) draft — maps + forwards; idempotency passthrough.</summary>
public sealed class RemoteCargoDrySettlementInvoiceServiceTests
{
    private static (RemoteCargoDrySettlementInvoiceService svc, ICargoDrySettlementPaymentRemoteCall remote) Build()
    {
        var remote = Substitute.For<ICargoDrySettlementPaymentRemoteCall>();
        var svc = new RemoteCargoDrySettlementInvoiceService(
            remote, NullLogger<RemoteCargoDrySettlementInvoiceService>.Instance);
        return (svc, remote);
    }

    private static Task<CreateCargoDrySettlementStatementResult> PrepareCall(
        RemoteCargoDrySettlementInvoiceService svc)
        => svc.PrepareSettlementStatementAsync(
            settlementId: 2, settlementCode: "STS-1", providerProfileId: 10, providerPayoutAmount: 60m,
            totalSaleAmount: 300m, totalCommissionAmount: 60m, totalKitCount: 2, currencyCode: "TRY",
            productCode: "STANDARD-90", periodStartUtc: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            periodEndUtc: new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc), payoutRecordId: 55,
            preparedByUserId: 7, notes: "n");

    [Fact]
    public async Task Forwards_details_and_maps_result()
    {
        var (svc, remote) = Build();
        remote.PrepareSettlementStatementAsync(Arg.Any<PrepareCargoDrySettlementStatementRemoteCallRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CreateCargoDrySettlementStatementResult(321, null, InvoiceStatus.Draft, AlreadyExisted: false));

        var r = await PrepareCall(svc);

        r.InvoiceId.Should().Be(321);
        r.InvoiceStatus.Should().Be(InvoiceStatus.Draft);
        r.AlreadyExisted.Should().BeFalse();
        await remote.Received(1).PrepareSettlementStatementAsync(
            Arg.Is<PrepareCargoDrySettlementStatementRemoteCallRequest>(x =>
                x.SettlementId == 2 && x.ProviderPayoutAmount == 60m && x.TotalKitCount == 2 && x.PayoutRecordId == 55),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Re_prepare_passes_through_already_existed()
    {
        var (svc, remote) = Build();
        remote.PrepareSettlementStatementAsync(Arg.Any<PrepareCargoDrySettlementStatementRemoteCallRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CreateCargoDrySettlementStatementResult(321, "INV-1", InvoiceStatus.Draft, AlreadyExisted: true));

        (await PrepareCall(svc)).AlreadyExisted.Should().BeTrue();
    }

    [Fact]
    public async Task Throws_when_remote_returns_null()
    {
        var (svc, remote) = Build();
        remote.PrepareSettlementStatementAsync(Arg.Any<PrepareCargoDrySettlementStatementRemoteCallRequest>(), Arg.Any<CancellationToken>())
            .Returns((CreateCargoDrySettlementStatementResult)null!);

        await svc.Invoking(_ => PrepareCall(svc)).Should().ThrowAsync<InvalidOperationException>();
    }
}
