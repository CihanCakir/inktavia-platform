using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Gateway;
using Aizen.Modules.Payment.Application.Gateway.Iyzico;
using Aizen.Modules.Payment.Application.Gateway.Iyzico.Models;
using FluentAssertions;

namespace Aizen.Modules.Payment.Domain.UnitTests.Gateway;

/// <summary>
/// BE-P9 — the pre-send split-math guard (§2) + snapshot basket builder (§3) + auth-mode resolver (§1). Pure, no HTTP.
/// Canonical smoke figures: Service 5000 @0.12 + Travel 800 exempt → ProviderNet 5200; platform fee gross 174 →
/// CustomerTotal 5974; retained = 774 (= commission 600 + fee 174 in VAT-free narrow core).
/// </summary>
public sealed class IyzicoSplitGuardAndBasketTests
{
    private const decimal CustomerTotal = 5974m;
    private const decimal ProviderNet   = 5200m;
    private const decimal FeeGross      = 174m;
    private const decimal Retained      = 774m;   // CustomerTotal − ProviderNet
    private const string  Key           = "SM-KEY";

    // ── Guard: correct single-split snapshot passes ─────────────────────────────

    [Fact]
    public void Guard_CorrectSingleSplit_Passes()
    {
        var basket = IyzicoBasketBuilder.BuildSingle("b1", "svc", CustomerTotal, ProviderNet, Key);

        var act = () => IyzicoSplitMathGuard.Verify(CustomerTotal, ProviderNet, basket, requireSplit: true, expectedRetained: Retained);
        act.Should().NotThrow();

        basket.Should().ContainSingle();
        basket[0].Price.Should().Be("5974.00");
        basket[0].SubMerchantPrice.Should().Be("5200.00");
        basket[0].SubMerchantKey.Should().Be(Key);
    }

    [Fact]
    public void Guard_NonMarketplace_NoKey_NoSplitRequired_Passes()
    {
        var basket = IyzicoBasketBuilder.BuildSingle("b1", "svc", CustomerTotal, ProviderNet, subMerchantKey: null);
        basket[0].SubMerchantPrice.Should().BeNull();

        var act = () => IyzicoSplitMathGuard.Verify(CustomerTotal, ProviderNet, basket, requireSplit: false);
        act.Should().NotThrow();
    }

    // ── Guard: each tamper fires IyzicoSplitMathMismatch ────────────────────────

    private static void ShouldThrowMismatch(Action act) =>
        act.Should().Throw<AizenBusinessException>()
           .Where(e => e.Message.Contains("Split-math guard"));

    [Fact]
    public void Guard_TamperedSubMerchantPrice_Throws()
    {
        var basket = IyzicoBasketBuilder.BuildSingle("b1", "svc", CustomerTotal, ProviderNet, Key);
        basket[0].SubMerchantPrice = "5199.00";   // != ProviderNet 5200
        ShouldThrowMismatch(() => IyzicoSplitMathGuard.Verify(CustomerTotal, ProviderNet, basket, requireSplit: true, expectedRetained: Retained));
    }

    [Fact]
    public void Guard_BasketSumNeqCustomerTotal_Throws()
    {
        var basket = IyzicoBasketBuilder.BuildSingle("b1", "svc", CustomerTotal, ProviderNet, Key);
        basket[0].Price = "5975.00";   // != CustomerTotal 5974
        ShouldThrowMismatch(() => IyzicoSplitMathGuard.Verify(CustomerTotal, ProviderNet, basket, requireSplit: true));
    }

    [Fact]
    public void Guard_MissingKeyOnSplitLine_Throws()
    {
        var basket = IyzicoBasketBuilder.BuildSingle("b1", "svc", CustomerTotal, ProviderNet, Key);
        basket[0].SubMerchantKey = null;   // split price present but no key
        ShouldThrowMismatch(() => IyzicoSplitMathGuard.Verify(CustomerTotal, ProviderNet, basket, requireSplit: true));
    }

    [Fact]
    public void Guard_RetainedMismatch_Throws()
        => ShouldThrowMismatch(() => IyzicoSplitMathGuard.Verify(
            CustomerTotal, ProviderNet,
            IyzicoBasketBuilder.BuildSingle("b1", "svc", CustomerTotal, ProviderNet, Key),
            requireSplit: true, expectedRetained: 999m));   // actual retained 774

    [Fact]
    public void Guard_ProviderNetExceedsCustomerTotal_Throws()
        => ShouldThrowMismatch(() => IyzicoSplitMathGuard.Verify(
            customerTotal: 1000m, providerNetTotal: 1200m,
            IyzicoBasketBuilder.BuildSingle("b1", "svc", 1000m, 1200m, Key),
            requireSplit: true));

    // ── Multi-item basket: Σ Price == CustomerTotal, Σ SubMerchantPrice == ProviderNet ──

    [Fact]
    public void MultiItemBasket_Sums_To_Snapshot_And_Guard_Passes()
    {
        // Service line 5000 → net 4400; Travel 800 exempt → net 800; + platform fee 174 retained.
        var lines = new List<IyzicoSplitLine>
        {
            new("L-SVC", "Service", LineTotal: 5000m, ProviderNet: 4400m),
            new("L-TRV", "Travel",  LineTotal: 800m,  ProviderNet: 800m),
        };
        var basket = IyzicoBasketBuilder.BuildMultiItem(lines, FeeGross, Key);

        basket.Should().HaveCount(3);                                   // 2 lines + platform fee
        basket.Sum(b => decimal.Parse(b.Price, System.Globalization.CultureInfo.InvariantCulture))
            .Should().Be(CustomerTotal);                                // 5000 + 800 + 174
        basket.Where(b => b.SubMerchantPrice != null)
            .Sum(b => decimal.Parse(b.SubMerchantPrice!, System.Globalization.CultureInfo.InvariantCulture))
            .Should().Be(ProviderNet);                                  // 4400 + 800
        basket.Single(b => b.Id == "PLATFORM-FEE").SubMerchantPrice.Should().BeNull();   // retained

        var act = () => IyzicoSplitMathGuard.Verify(CustomerTotal, ProviderNet, basket, requireSplit: true, expectedRetained: Retained);
        act.Should().NotThrow();
    }

    // ── Auth-mode resolver ──────────────────────────────────────────────────────

    [Fact]
    public void AuthMode_Default_Is_Capture()
        => PaymentAuthModeResolver.Resolve("ENGINE", new PaymentAuthModeOptions())
            .Should().Be(PaymentAuthMode.Capture);

    [Fact]
    public void AuthMode_CategoryOverride_Wins_CaseInsensitive()
    {
        var opts = new PaymentAuthModeOptions
        {
            Default = PaymentAuthMode.Capture,
            CategoryOverrides = new() { ["ENGINE"] = PaymentAuthMode.PreAuth },
        };
        PaymentAuthModeResolver.Resolve("engine", opts).Should().Be(PaymentAuthMode.PreAuth);
        PaymentAuthModeResolver.Resolve("HULL", opts).Should().Be(PaymentAuthMode.Capture);   // no override → default
        PaymentAuthModeResolver.Resolve(null, opts).Should().Be(PaymentAuthMode.Capture);
    }
}
