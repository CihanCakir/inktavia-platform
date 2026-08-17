using System.Globalization;
using Aizen.Modules.Payment.Application.Gateway.Iyzico;
using Aizen.Modules.Payment.Application.Gateway.Iyzico.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aizen.Modules.Payment.Domain.UnitTests.Gateway;

/// <summary>
/// BE-P9 PRODUCTION GATE — LIVE iyzico sandbox split verification. <b>Skipped unless real sandbox keys are supplied via env
/// AND opted-in</b> (never hardcoded, never committed). Run with:
/// <code>
/// export Iyzico__ApiKey=... Iyzico__SecretKey=... IYZICO_LIVE_SANDBOX=1
/// dotnet test --filter Category=LiveSandbox
/// </code>
/// This automated portion covers the network-verifiable steps: (1) register a sandbox sub-merchant → real SubMerchantKey;
/// (2) initialize a checkout form carrying the split basket (Price=CustomerTotal, SubMerchantPrice=ProviderNet) and confirm
/// iyzico accepts it (returns a form token). Completing the card payment + webhook capture + ReleaseEscrow (approve) and
/// reading back price/subMerchantPrice/retained on the dashboard/API is the manual step documented in REPORT_BACKEND.md.
/// </summary>
[Trait("Category", "LiveSandbox")]
public sealed class IyzicoLiveSandboxSplitTests
{
    // Canonical gate figures: Service 5000 @0.12 + Travel 800 exempt → ProviderNet 5200; +platform fee → CustomerTotal.
    private const decimal CustomerTotal = 5974m;
    private const decimal ProviderNet   = 5200m;

    private static IyzicoConfiguration? LiveConfig()
    {
        if (Environment.GetEnvironmentVariable("IYZICO_LIVE_SANDBOX") != "1") return null;
        var apiKey = Environment.GetEnvironmentVariable("Iyzico__ApiKey");
        var secret = Environment.GetEnvironmentVariable("Iyzico__SecretKey");
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(secret)
            || apiKey.Contains("placeholder")) return null;

        return new IyzicoConfiguration
        {
            ApiKey    = apiKey,
            SecretKey = secret,
            BaseUrl   = Environment.GetEnvironmentVariable("Iyzico__BaseUrl") ?? "https://sandbox-api.iyzipay.com",
            Locale    = "tr",
        };
    }

    private static IyzicoHttpClient NewClient(IyzicoConfiguration cfg)
    {
        var http = new HttpClient { BaseAddress = new Uri(cfg.BaseUrl) };
        return new IyzicoHttpClient(http, Options.Create(cfg), NullLogger<IyzicoHttpClient>.Instance);
    }

    [Fact]
    public async Task LiveSandbox_Register_SubMerchant_Then_Init_Split_Checkout()
    {
        var cfg = LiveConfig();
        if (cfg is null) return;   // no keys / not opted-in → skip silently (no network, secrets never touched)

        var client = NewClient(cfg);
        var stamp  = Guid.NewGuid().ToString("N")[..8];

        // 1) Register a sandbox sub-merchant → real SubMerchantKey.
        var sm = await client.CreateSubMerchantAsync(new IyzicoSubMerchantRequest
        {
            Locale = "tr", ConversationId = $"SM-{stamp}",
            SubMerchantExternalId = $"PROV-{stamp}", SubMerchantType = "PRIVATE_COMPANY",
            Address = "Test Mah. Test Cad. No:1 Istanbul", ContactName = "Test", ContactSurname = "Provider",
            Email = $"prov-{stamp}@inktavia.test", GsmNumber = "+905350000000",
            Name = "Test Marine Ltd", Iban = "TR180006200119000006672315",
            TaxOffice = "Kadikoy", TaxNumber = "1234567890", LegalCompanyTitle = "Test Marine Ltd", Currency = "TRY",
        });

        sm.Should().NotBeNull();
        sm!.IsSuccess.Should().BeTrue($"iyzico sub-merchant create failed: [{sm.ErrorCode}] {sm.ErrorMessage}");
        sm.SubMerchantKey.Should().NotBeNullOrEmpty();

        // 2) Pre-send guard on the split basket (must pass on the correct case).
        var basket = IyzicoBasketBuilder.BuildSingle($"TXN-{stamp}", "Marine Service", CustomerTotal, ProviderNet, sm.SubMerchantKey);
        IyzicoSplitMathGuard.Verify(CustomerTotal, ProviderNet, basket, requireSplit: true, expectedRetained: CustomerTotal - ProviderNet);

        // 3) Initialize the checkout form carrying the split → iyzico accepts (returns a token).
        var form = await client.InitializeCheckoutFormAsync(new IyzicoCheckoutFormRequest
        {
            Locale = "tr", ConversationId = $"SR-LIVE-{stamp}",
            Price = CustomerTotal.ToString("F2", CultureInfo.InvariantCulture),
            PaidPrice = CustomerTotal.ToString("F2", CultureInfo.InvariantCulture),
            Currency = "TRY", BasketId = $"TXN-{stamp}", PaymentGroup = "PRODUCT",
            CallbackUrl = "https://localhost:3000/payment/callback",
            Buyer = new IyzicoBuyer { Id = "1", Name = "Test", Surname = "Buyer", Email = "buyer@inktavia.test" },
            ShippingAddress = new IyzicoAddress { ContactName = "Test Buyer" },
            BillingAddress  = new IyzicoAddress { ContactName = "Test Buyer" },
            BasketItems = basket,
        });

        form.Should().NotBeNull();
        form!.IsSuccess.Should().BeTrue($"iyzico checkout init rejected the split basket: [{form.ErrorCode}] {form.ErrorMessage}");
        form.Token.Should().NotBeNullOrEmpty("a valid form token proves iyzico accepted the split payload");
    }
}
