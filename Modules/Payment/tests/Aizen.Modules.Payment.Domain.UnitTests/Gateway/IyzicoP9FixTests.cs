using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Model;
using Aizen.Modules.Payment.Application.Gateway.Iyzico;
using Aizen.Modules.Payment.Application.Gateway.Iyzico.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Payment.Domain.UnitTests.Gateway;

/// <summary>
/// BE-P9-fix — iyzico API alignment: IYZWSv2 signing (§1), item-approve (§4), webhook V3 (§2), response signature (§3),
/// type-varied sub-merchant (§5), refund shape (§8). All proven with documented vectors + a mock HttpMessageHandler; no keys.
/// </summary>
public sealed class IyzicoP9FixTests
{
    private static string Hex(string key, string data)
        => Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(data))).ToLowerInvariant();

    // ── §1 IYZWSv2 signing (randomKey + uriPath + body → HEX; apiKey:&randomKey:&signature: → base64) ──

    [Fact]
    public void Signing_HashesRandomKeyPlusPathPlusBody_AsLowercaseHex()
    {
        // The documented bin/check example body + path (the exact byte-string the client serializes).
        const string body = "{\"locale\":\"tr\",\"binNumber\":\"535805\",\"conversationId\":\"docsTest-v1\"}";
        const string path = "/payment/bin/check";
        const string secret = "test-secret-key", rnd = "test-random-key";

        var sig = IyzicoSignatureHelper.ComputeRequestSignature(secret, path, rnd, body);

        // byte-exact algorithm: HEX_lower(HMACSHA256(secret, randomKey + uriPath + body))
        sig.Should().Be(Hex(secret, rnd + path + body));
        sig.Should().MatchRegex("^[0-9a-f]{64}$");                       // HEX lowercase, NOT base64
        sig.Should().NotBe(Convert.ToBase64String(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(secret + rnd + body))));
    }

    [Fact]
    public void Authorization_Header_DecodesTo_ApiKeyRandomKeySignature_Form()
    {
        const string body = "{}", path = "/payment/iyzipos/item/approve";
        const string apiKey = "API-123", secret = "S", rnd = "R";

        var header = IyzicoSignatureHelper.BuildAuthorizationHeader(apiKey, secret, path, rnd, body);

        header.Should().StartWith("IYZWSv2 ");
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header["IYZWSv2 ".Length..]));
        decoded.Should().Be($"apiKey:{apiKey}&randomKey:{rnd}&signature:{Hex(secret, rnd + path + body)}");
        decoded.Should().Contain("&randomKey:").And.Contain("&signature:");   // NOT the old "apiKey:rnd:hmac" form
    }

    // ── §2 webhook V3 (HPP) ─────────────────────────────────────────────────────

    [Fact]
    public void WebhookV3_Hpp_Signature_Matches_And_WrongRejected()
    {
        const string secret = "wh-secret", evt = "CHECKOUT_FORM_AUTH", pid = "P9", token = "TOK", conv = "SR-1-OFFER-1", status = "SUCCESS";
        var expected = Hex(secret, secret + evt + pid + token + conv + status);

        IyzicoSignatureHelper.ComputeHppWebhookSignatureV3(secret, evt, pid, token, conv, status).Should().Be(expected);
        IyzicoSignatureHelper.ValidateHppWebhookSignatureV3(secret, evt, pid, token, conv, status, expected).Should().BeTrue();
        IyzicoSignatureHelper.ValidateHppWebhookSignatureV3(secret, evt, pid, token, conv, status, "deadbeef").Should().BeFalse();
    }

    [Fact]
    public void WebhookV3_DevBypass_OffInProduction()
    {
        var client = NewClient(secret: "");   // no secret
        client.ValidateHppWebhookSignatureV3("CHECKOUT_FORM_AUTH", "P", "T", "C", "SUCCESS", headerSignature: null, isProduction: false).Should().BeTrue();
        client.ValidateHppWebhookSignatureV3("CHECKOUT_FORM_AUTH", "P", "T", "C", "SUCCESS", headerSignature: null, isProduction: true).Should().BeFalse();
    }

    // ── §3 response signature (trailing-zero trim + join order) ─────────────────

    [Theory]
    [InlineData("10.50", "10.5")]
    [InlineData("10.0", "10")]
    [InlineData("10", "10")]
    [InlineData("0.00", "0")]
    [InlineData("174.00", "174")]
    public void ResponseSig_TrimsTrailingZeros(string input, string expected)
        => IyzicoSignatureHelper.TrimPrice(input).Should().Be(expected);

    [Fact]
    public void ResponseSig_Refund_JoinOrder_KnownAnswer()
    {
        const string secret = "s";
        var expected = Hex(secret, string.Join(":", "PID", "10.5", "TRY", "CONV"));   // paymentId:price:currency:conversationId
        IyzicoSignatureHelper.ComputeResponseSignature(secret, "PID", IyzicoSignatureHelper.TrimPrice("10.50"), "TRY", "CONV").Should().Be(expected);
        IyzicoSignatureHelper.ValidateResponseSignature(secret, expected, "PID", "10.5", "TRY", "CONV").Should().BeTrue();
        IyzicoSignatureHelper.ValidateResponseSignature(secret, "bad", "PID", "10.5", "TRY", "CONV").Should().BeFalse();
    }

    // ── §5 sub-merchant: type-varied bodies + fail-loud + no hardcoded TCKN + IBAN omission ──

    private static string Serialize(IyzicoSubMerchantRequest r)
        => JsonSerializer.Serialize(r, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    private static SubMerchantOnboardingData Data(string type, string? tckn = null, string? taxOffice = null, string? taxNumber = null, string? iban = null)
        => new(type, "PROV-1", "Acme", "a@b.com", "Addr", "+9053", "Ada", "Lovelace", tckn, taxOffice, taxNumber, "Acme Ltd", iban, "SM-1");

    [Fact]
    public void SubMerchant_Personal_SerializesTckn_NotTaxFields()
    {
        var json = Serialize(IyzicoSubMerchantRequestBuilder.Build(Data(IyzicoSubMerchantType.Personal, tckn: "12345678901")));
        json.Should().Contain("\"identityNumber\":\"12345678901\"").And.Contain("\"contactName\"");
        json.Should().NotContain("taxOffice").And.NotContain("legalCompanyTitle").And.NotContain("11111111111");
    }

    [Fact]
    public void SubMerchant_Personal_MissingTckn_FailsLoud()
    {
        var act = () => IyzicoSubMerchantRequestBuilder.Build(Data(IyzicoSubMerchantType.Personal, tckn: null));
        act.Should().Throw<AizenBusinessException>().WithMessage("*identityNumber*");
    }

    [Fact]
    public void SubMerchant_PrivateCompany_RequiresTaxOfficeAndTitle_NoTckn()
    {
        var json = Serialize(IyzicoSubMerchantRequestBuilder.Build(Data(IyzicoSubMerchantType.PrivateCompany, taxOffice: "Kadikoy")));
        json.Should().Contain("\"taxOffice\":\"Kadikoy\"").And.Contain("legalCompanyTitle");
        json.Should().NotContain("identityNumber");
    }

    [Fact]
    public void SubMerchant_Limited_RequiresTaxNumber()
    {
        var act = () => IyzicoSubMerchantRequestBuilder.Build(Data(IyzicoSubMerchantType.LimitedOrJointStock, taxOffice: "Kadikoy", taxNumber: null));
        act.Should().Throw<AizenBusinessException>().WithMessage("*taxNumber*");
        Serialize(IyzicoSubMerchantRequestBuilder.Build(Data(IyzicoSubMerchantType.LimitedOrJointStock, taxOffice: "Kadikoy", taxNumber: "1234567890")))
            .Should().Contain("\"taxNumber\":\"1234567890\"");
    }

    [Fact]
    public void SubMerchant_NoIban_Omitted()
        => Serialize(IyzicoSubMerchantRequestBuilder.Build(Data(IyzicoSubMerchantType.PrivateCompany, taxOffice: "K", iban: null)))
            .Should().NotContain("\"iban\"");

    // ── §8 refund shape ─────────────────────────────────────────────────────────

    [Fact]
    public void Refund_Reasons_AreIyzicoEnum()
    {
        IyzicoRefundReason.Other.Should().Be("OTHER");
        IyzicoRefundReason.BuyerRequest.Should().Be("BUYER_REQUEST");
        IyzicoRefundReason.DoublePayment.Should().Be("DOUBLE_PAYMENT");
        IyzicoRefundReason.Fraud.Should().Be("FRAUD");
    }

    // ── §4 approve path: ReleaseEscrow hits /payment/iyzipos/item/approve with the item id ──

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public readonly List<(string Path, string Body)> Calls = new();
        public string Response = "{\"status\":\"success\",\"paymentTransactionId\":\"PTX-1\"}";
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
        {
            Calls.Add((req.RequestUri!.AbsolutePath, req.Content is null ? "" : await req.Content.ReadAsStringAsync(ct)));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Response, Encoding.UTF8, "application/json") };
        }
    }

    private static IyzicoHttpClient NewClient(string secret = "S")
    {
        var http = new HttpClient(new RecordingHandler()) { BaseAddress = new Uri("https://sandbox-api.iyzipay.com") };
        return new IyzicoHttpClient(http, Options.Create(new IyzicoConfiguration { ApiKey = "k", SecretKey = secret, BaseUrl = "https://sandbox-api.iyzipay.com" }), NullLogger<IyzicoHttpClient>.Instance);
    }

    [Fact]
    public async Task ReleaseEscrow_Uses_ItemApprove_Endpoint_WithStored_ItemTxId()
    {
        var handler = new RecordingHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://sandbox-api.iyzipay.com") };
        var cfg = Options.Create(new IyzicoConfiguration { ApiKey = "k", SecretKey = "s", BaseUrl = "https://sandbox-api.iyzipay.com", Locale = "tr" });
        var client = new IyzicoHttpClient(http, cfg, NullLogger<IyzicoHttpClient>.Instance);
        var provider = new IyzicoMarketplacePaymentGatewayProvider(client, cfg, NullLogger<IyzicoMarketplacePaymentGatewayProvider>.Instance);

        var result = await provider.ReleaseEscrowAsync(new ReleaseEscrowInput
        {
            TransactionId = 1, GatewayReference = "TOKEN-abc", GatewayItemTransactionId = "PTX-1", ProviderNetAmount = 5200m,
        });

        result.Processed.Should().BeTrue();
        handler.Calls.Should().ContainSingle();
        handler.Calls[0].Path.Should().Be("/payment/iyzipos/item/approve");   // NOT /payment/marketplace/approval
        handler.Calls[0].Body.Should().Contain("\"paymentTransactionId\":\"PTX-1\"");   // the stored item id, not the token
    }

    [Fact]
    public async Task ReleaseEscrow_NoStoredItemTxId_DoesNotCallGateway()
    {
        var handler = new RecordingHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://sandbox-api.iyzipay.com") };
        var cfg = Options.Create(new IyzicoConfiguration { ApiKey = "k", SecretKey = "s", BaseUrl = "https://sandbox-api.iyzipay.com" });
        var provider = new IyzicoMarketplacePaymentGatewayProvider(
            new IyzicoHttpClient(http, cfg, NullLogger<IyzicoHttpClient>.Instance), cfg, NullLogger<IyzicoMarketplacePaymentGatewayProvider>.Instance);

        var result = await provider.ReleaseEscrowAsync(new ReleaseEscrowInput
        {
            TransactionId = 1, GatewayReference = "TOKEN", GatewayItemTransactionId = null, ProviderNetAmount = 100m,
        });

        result.Processed.Should().BeFalse();
        handler.Calls.Should().BeEmpty();
    }
}
