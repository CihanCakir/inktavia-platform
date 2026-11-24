using System.Net;
using System.Text;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Model;
using Aizen.Modules.Payment.Application.Gateway.Iyzico;
using Aizen.Modules.Payment.Application.Gateway.Iyzico.Models;
using Aizen.Modules.Payment.Application.Services;
using Aizen.Modules.Payment.Domain.Entities.Transaction;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aizen.Modules.Payment.Domain.UnitTests.Gateway;

/// <summary>
/// FIX_ESCROW_CAPTURE_AT_INIT — an async gateway (Iyzico) must NOT capture the escrow transaction at checkout-init:
/// the tx stays PendingIntent (CapturedAt == null) so the later payment webhook can capture it AND record the
/// sub-merchant item breakdown. A synchronously-settling gateway (manual, no webhook) still captures at init.
///
/// Covers: (1) the two domain transitions the create-branch selects between (AttachGatewayReference vs Capture);
/// (2) each gateway provider's <see cref="CheckoutInitResult.CapturedAtInit"/> flag that drives that selection.
/// The escrow-create branch itself is <c>if (initResult.CapturedAtInit) tx.Capture(ref) else tx.AttachGatewayReference(ref)</c>
/// — proven here by composition (correct flag per gateway × correct persisted state per method). See
/// EscrowWebhookCaptureFixTests for the webhook capture + breakdown + idempotency over a real repository.
/// </summary>
public sealed class EscrowCaptureAtInitTests
{
    private static PaymentTransactionEntity NewPendingTx(string gateway = "iyzico") =>
        PaymentTransactionEntity.Create(
            transactionCode: "TXN-CAPINIT-1", transactionType: TransactionType.ServiceRequestEscrow,
            contextType: TransactionContextType.ServiceRequest, contextId: 1, contextSubId: 1,
            payerProfileId: 100, recipientProfileId: 200, grossAmount: 5974m,
            commissionAmount: 600m, commissionRateSnapshot: 0.12m, vatOnCommission: 120m, netPayoutAmount: 5200m,
            discountAmount: 0m, currencyCode: "TRY", gatewayProvider: gateway,
            idempotencyKey: "SR-1-OFFER-1", escrowRequired: true);

    // ── Domain transitions the create-branch selects between ────────────────────

    [Fact]
    public void AttachGatewayReference_KeepsPendingIntent_AndNullCapturedAt_WhileSettingReference()
    {
        // The async-gateway branch: reference is attached, money is NOT collected yet.
        var tx = NewPendingTx();

        tx.AttachGatewayReference("CHECKOUT-TOKEN-abc");

        tx.Status.Should().Be(PaymentTransactionStatus.PendingIntent);   // webhook's `if (CapturedAt.HasValue)` guard is FALSE → it proceeds
        tx.CapturedAt.Should().BeNull();
        tx.GatewayReference.Should().Be("CHECKOUT-TOKEN-abc");
    }

    [Fact]
    public void Capture_SetsCaptured_AndNonNullCapturedAt_AndReference()
    {
        // The sync-gateway (manual) branch: settled immediately at init.
        var tx = NewPendingTx("manual");

        tx.Capture("MANUAL-SR-1-OFFER-1");

        tx.Status.Should().Be(PaymentTransactionStatus.Captured);
        tx.CapturedAt.Should().NotBeNull();
        tx.GatewayReference.Should().Be("MANUAL-SR-1-OFFER-1");
    }

    // ── Gateway providers: the CapturedAtInit flag that drives the branch ────────

    [Fact]
    public async Task ManualProvider_Init_Returns_CapturedAtInit_True()
    {
        var provider = new ManualPaymentGatewayProvider(NullLogger<ManualPaymentGatewayProvider>.Instance);

        var result = await provider.InitiateCheckoutAsync(ManualInput());

        result.IsSuccess.Should().BeTrue();
        result.CapturedAtInit.Should().BeTrue("manual settles synchronously — no webhook to capture later");
    }

    [Fact]
    public async Task IyzicoProvider_Init_Success_Returns_CapturedAtInit_False()
    {
        // Mock a successful checkout-form-initialize response → the provider returns a form token but CapturedAtInit=false.
        var handler = new StubHandler("{\"status\":\"success\",\"token\":\"IYZ-TOKEN-1\",\"checkoutFormContent\":\"<html>form</html>\"}");
        var provider = NewIyzicoProvider(handler);

        var result = await provider.InitiateCheckoutAsync(IyzicoInput());

        result.IsSuccess.Should().BeTrue();
        result.GatewayReference.Should().Be("IYZ-TOKEN-1");
        result.CapturedAtInit.Should().BeFalse("real capture happens in the payment webhook, not at init");
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private static CheckoutInitInput ManualInput() => new()
    {
        TransactionId = 1, IdempotencyKey = "SR-1-OFFER-1", GrossAmount = 5974m, CurrencyCode = "TRY",
        PayerProfileId = 100, RecipientProfileId = 200, SubMerchantKey = null, ProviderNetAmount = 5200m,
        Context = TransactionContext.ForServiceRequest(1, 1), EscrowRequired = true,
    };

    // No SubMerchantKey → no split required → the pre-send split-math guard passes with a single basket item.
    private static CheckoutInitInput IyzicoInput() => new()
    {
        TransactionId = 1, IdempotencyKey = "SR-1-OFFER-1", GrossAmount = 5974m, CurrencyCode = "TRY",
        PayerProfileId = 100, RecipientProfileId = 200, SubMerchantKey = null, ProviderNetAmount = 5974m,
        Context = TransactionContext.ForServiceRequest(1, 1), EscrowRequired = true,
        Description = "Service Request Payment",
    };

    private static IyzicoMarketplacePaymentGatewayProvider NewIyzicoProvider(HttpMessageHandler handler)
    {
        var cfg = Options.Create(new IyzicoConfiguration
        {
            ApiKey = "k", SecretKey = "s", BaseUrl = "https://sandbox-api.iyzipay.com", Locale = "tr",
            CallbackUrl = "https://localhost/payment/callback",
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://sandbox-api.iyzipay.com") };
        var client = new IyzicoHttpClient(http, cfg, NullLogger<IyzicoHttpClient>.Instance);
        return new IyzicoMarketplacePaymentGatewayProvider(client, cfg, NullLogger<IyzicoMarketplacePaymentGatewayProvider>.Instance);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly string _json;
        public StubHandler(string json) => _json = json;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_json, Encoding.UTF8, "application/json"),
            });
    }
}
