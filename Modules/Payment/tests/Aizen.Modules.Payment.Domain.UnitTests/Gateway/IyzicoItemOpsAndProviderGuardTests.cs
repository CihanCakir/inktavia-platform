using System.Net;
using System.Text;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Model;
using Aizen.Modules.Payment.Application.Gateway.Iyzico;
using Aizen.Modules.Payment.Application.Gateway.Iyzico.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Payment.Domain.UnitTests.Gateway;

/// <summary>
/// BE-P9 — the item-level client methods hit the right endpoints with typed payloads (mocked HTTP), and the gateway
/// provider runs the pre-send guard BEFORE any client call (a tampered split → mismatch, ZERO sends).
/// </summary>
public sealed class IyzicoItemOpsAndProviderGuardTests
{
    /// <summary>Records every outgoing request and returns a canned iyzico success JSON.</summary>
    private sealed class RecordingHandler : HttpMessageHandler
    {
        public readonly List<(string Method, string Path, string Body)> Calls = new();
        public string ResponseJson = """{"status":"success","token":"TOK-1","paymentTransactionId":"PTX-1"}""";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(ct);
            Calls.Add((request.Method.Method, request.RequestUri!.AbsolutePath, body));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(ResponseJson, Encoding.UTF8, "application/json"),
            };
        }
    }

    private static (IyzicoHttpClient client, RecordingHandler handler) NewClient()
    {
        var handler = new RecordingHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://sandbox-api.iyzipay.com") };
        var cfg  = Options.Create(new IyzicoConfiguration { ApiKey = "k", SecretKey = "s", BaseUrl = "https://sandbox-api.iyzipay.com" });
        return (new IyzicoHttpClient(http, cfg, NullLogger<IyzicoHttpClient>.Instance), handler);
    }

    // ── Item-level client methods hit the right endpoints ───────────────────────

    [Fact]
    public async Task ApproveItem_Posts_To_ItemApprove_Endpoint()
    {
        var (client, handler) = NewClient();
        var resp = await client.ApproveItemAsync(new IyzicoItemApproveRequest { PaymentTransactionId = "PTX-1" });

        resp!.IsSuccess.Should().BeTrue();
        handler.Calls.Should().ContainSingle();
        handler.Calls[0].Method.Should().Be("POST");
        handler.Calls[0].Path.Should().Be("/payment/iyzipos/item/approve");
        handler.Calls[0].Body.Should().Contain("\"paymentTransactionId\":\"PTX-1\"");
    }

    [Fact]
    public async Task DisapproveItem_Posts_To_ItemDisapprove_Endpoint()
    {
        var (client, handler) = NewClient();
        await client.DisapproveItemAsync(new IyzicoItemDisapproveRequest { PaymentTransactionId = "PTX-9" });
        handler.Calls[0].Method.Should().Be("POST");
        handler.Calls[0].Path.Should().Be("/payment/iyzipos/item/disapprove");
    }

    [Fact]
    public async Task UpdateSubMerchantShare_Puts_To_PaymentItem_Endpoint()
    {
        var (client, handler) = NewClient();
        await client.UpdateSubMerchantShareAsync(new IyzicoUpdateItemRequest
        {
            PaymentTransactionId = "PTX-1", SubMerchantKey = "SM", SubMerchantPrice = "4400.00",
        });
        handler.Calls[0].Method.Should().Be("PUT");
        handler.Calls[0].Path.Should().Be("/payment/item");
        handler.Calls[0].Body.Should().Contain("\"subMerchantPrice\":\"4400.00\"");
    }

    [Fact]
    public async Task ItemOp_Failure_Surfaces_NotSuccess()
    {
        var (client, handler) = NewClient();
        handler.ResponseJson = """{"status":"failure","errorCode":"5","errorMessage":"nope"}""";
        var resp = await client.ApproveItemAsync(new IyzicoItemApproveRequest { PaymentTransactionId = "X" });
        resp!.IsSuccess.Should().BeFalse();
        resp.ErrorMessage.Should().Be("nope");
    }

    // ── Gateway provider: pre-send guard runs BEFORE any client call ────────────

    private static IyzicoMarketplacePaymentGatewayProvider NewProvider(RecordingHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://sandbox-api.iyzipay.com") };
        var cfg  = Options.Create(new IyzicoConfiguration { ApiKey = "k", SecretKey = "s", BaseUrl = "https://sandbox-api.iyzipay.com", Locale = "tr" });
        var client = new IyzicoHttpClient(http, cfg, NullLogger<IyzicoHttpClient>.Instance);
        return new IyzicoMarketplacePaymentGatewayProvider(client, cfg, NullLogger<IyzicoMarketplacePaymentGatewayProvider>.Instance);
    }

    private static CheckoutInitInput Input(decimal gross, decimal net, string? key, decimal? retained) => new()
    {
        TransactionId = 1, IdempotencyKey = "SR-1-OFFER-1", GrossAmount = gross, CurrencyCode = "TRY",
        PayerProfileId = 7, RecipientProfileId = 9, ProviderNetAmount = net, SubMerchantKey = key,
        ExpectedRetainedAmount = retained, Context = TransactionContext.ForServiceRequest(1, 1),
        EscrowRequired = true, Description = "svc",
    };

    [Fact]
    public async Task Provider_CorrectSplit_CallsCheckout_WithSplitBasket()
    {
        var handler = new RecordingHandler();
        var provider = NewProvider(handler);

        var result = await provider.InitiateCheckoutAsync(Input(gross: 5974m, net: 5200m, key: "SM-KEY", retained: 774m));

        result.IsSuccess.Should().BeTrue();
        handler.Calls.Should().ContainSingle();
        handler.Calls[0].Path.Should().Be("/payment/iyzipos/checkoutform/initialize/auth/ecom");
        handler.Calls[0].Body.Should().Contain("\"subMerchantKey\":\"SM-KEY\"");
        handler.Calls[0].Body.Should().Contain("\"subMerchantPrice\":\"5200.00\"");
        handler.Calls[0].Body.Should().Contain("\"price\":\"5974.00\"");
    }

    [Fact]
    public async Task Provider_TamperedRetained_Throws_And_MakesNoCall()
    {
        var handler = new RecordingHandler();
        var provider = NewProvider(handler);

        // retained cross-check fails (actual 774, claimed 999) → guard throws BEFORE the client call.
        var act = async () => await provider.InitiateCheckoutAsync(Input(gross: 5974m, net: 5200m, key: "SM-KEY", retained: 999m));

        await act.Should().ThrowAsync<AizenBusinessException>();
        handler.Calls.Should().BeEmpty("the pre-send guard must fire before any iyzico call");
    }
}
