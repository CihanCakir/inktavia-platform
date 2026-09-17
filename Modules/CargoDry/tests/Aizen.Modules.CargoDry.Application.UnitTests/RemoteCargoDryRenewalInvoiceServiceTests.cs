using Aizen.Modules.CargoDry.Application.Services;
using Aizen.Modules.Payment.Abstraction.RemoteCall;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Aizen.Modules.CargoDry.Application.UnitTests;

/// <summary>Split-host HTTP bridge for the kit renewal invoice draft — forwards args, returns the created invoice id.</summary>
public sealed class RemoteCargoDryRenewalInvoiceServiceTests
{
    private static (RemoteCargoDryRenewalInvoiceService svc, ICargoDrySettlementPaymentRemoteCall remote) Build()
    {
        var remote = Substitute.For<ICargoDrySettlementPaymentRemoteCall>();
        var svc = new RemoteCargoDryRenewalInvoiceService(
            remote, NullLogger<RemoteCargoDryRenewalInvoiceService>.Instance);
        return (svc, remote);
    }

    private static Task<long> PrepareCall(RemoteCargoDryRenewalInvoiceService svc)
        => svc.PrepareRenewalInvoiceAsync(
            renewalPreparationId: 88, renewalCode: "RNW-1", kitId: 5, kitCode: "KIT-5", productCode: "STANDARD-90",
            productName: "Standard Kit", ownerUserId: 100, renewalPrice: 149.99m, currencyCode: "TRY",
            renewalMonths: 3, note: "n");

    [Fact]
    public async Task Forwards_args_and_returns_invoice_id()
    {
        var (svc, remote) = Build();
        remote.PrepareRenewalInvoiceAsync(Arg.Any<PrepareCargoDryRenewalInvoiceRemoteCallRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PrepareCargoDryRenewalInvoiceRemoteCallResponse { InvoiceId = 7777 });

        var id = await PrepareCall(svc);

        id.Should().Be(7777);
        await remote.Received(1).PrepareRenewalInvoiceAsync(
            Arg.Is<PrepareCargoDryRenewalInvoiceRemoteCallRequest>(x =>
                x.RenewalPreparationId == 88 && x.KitCode == "KIT-5" && x.RenewalPrice == 149.99m && x.RenewalMonths == 3),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_when_remote_returns_null()
    {
        var (svc, remote) = Build();
        remote.PrepareRenewalInvoiceAsync(Arg.Any<PrepareCargoDryRenewalInvoiceRemoteCallRequest>(), Arg.Any<CancellationToken>())
            .Returns((PrepareCargoDryRenewalInvoiceRemoteCallResponse)null!);

        await svc.Invoking(_ => PrepareCall(svc)).Should().ThrowAsync<InvalidOperationException>();
    }
}
