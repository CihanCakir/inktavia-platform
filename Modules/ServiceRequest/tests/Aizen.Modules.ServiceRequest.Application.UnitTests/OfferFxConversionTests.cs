using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.RemoteCall;
using Aizen.Modules.ServiceRequest.Application.Services;
using Aizen.Modules.ServiceRequest.Application.Services.Fx;
using Aizen.Modules.ServiceRequest.Domain.Entities.Offer;
using FluentAssertions;

namespace Aizen.Modules.ServiceRequest.Application.UnitTests;

/// <summary>
/// BE-S3 — submit-time FX. Proves: (1) a TRY-only offer resolves NOTHING (no remote call) and its economics are byte-identical
/// to pre-S3; (2) a foreign line converts at the resolved rate and the TRY economics are correct; (3) a missing rate fails loud
/// with SR_FX_RATE_UNAVAILABLE (never a silent 0/1.0); (5) the conversion rounding matches S1's money convention exactly. The
/// acceptance-freeze (4) is proven Payment-side in OfferLineFxSnapshotTests.
/// </summary>
public sealed class OfferFxConversionTests
{
    private static readonly OfferCalculationService Calc = new();

    // ── A trivial fake rate source (the interface is one method) — counts calls, returns canned rates ──
    private sealed class FakeRateSource : IExchangeRateSource
    {
        private readonly Dictionary<string, decimal?> _rates;   // source ccy → rate (null ⇒ no effective rate)
        public int Calls { get; private set; }
        public FakeRateSource(Dictionary<string, decimal?> rates) => _rates = rates;

        public Task<SrExchangeRateResolveDto?> GetAsync(string from, string to, DateTimeOffset asOfUtc, CancellationToken ct)
        {
            Calls++;
            var has = _rates.TryGetValue(from.ToUpperInvariant(), out var rate) && rate is > 0m;
            return Task.FromResult<SrExchangeRateResolveDto?>(new SrExchangeRateResolveDto
            {
                HasRate = has, FromCurrencyCode = from, ToCurrencyCode = to,
                Rate = rate ?? 0m, RateDate = new DateTime(2026, 8, 5, 0, 0, 0, DateTimeKind.Utc), AsOfUtc = asOfUtc,
            });
        }
    }

    private static ServiceRequestOfferEntity OfferWith(params ServiceRequestOfferItemEntity[] items)
    {
        var offer = ServiceRequestOfferEntity.Create(1, 2, 3, 0m, "TRY", null, null, null, null, null, null);
        foreach (var i in items) offer.AddItem(i);
        return offer;
    }

    private static ServiceRequestOfferItemEntity Line(string currency, decimal unitPrice, decimal qty = 1m, decimal taxRate = 0m)
        => ServiceRequestOfferItemEntity.Create(0, ServiceRequestOfferItemType.Service, "svc", null, qty, unitPrice, currency, 0,
            null, taxRate);

    // ── (1) TRY-only offer → no resolve call, no conversion, economics byte-identical ─────────────────
    [Fact]
    public async Task TryOnly_Offer_MakesNoResolveCall_AndIsUnchanged()
    {
        var fake = new FakeRateSource(new() { ["EUR"] = 42.60m });   // available but must NOT be used
        var resolver = new OfferFxResolver(fake);

        var offer   = OfferWith(Line("TRY", 1000m, taxRate: 0.20m), Line("TRY", 250m, qty: 2m));
        var control = OfferWith(Line("TRY", 1000m, taxRate: 0.20m), Line("TRY", 250m, qty: 2m));

        await resolver.ResolveAndConvertAsync(offer, DateTimeOffset.UtcNow, default);
        Calc.Calculate(offer);
        Calc.Calculate(control);

        fake.Calls.Should().Be(0, "a TRY-only offer needs no FX resolve");
        offer.FxSnapshots.Should().BeEmpty();
        offer.Items.Should().OnlyContain(i => i.SourceUnitPrice == null);
        offer.GrandTotal.Should().Be(control.GrandTotal);
        offer.Subtotal.Should().Be(control.Subtotal);
        offer.TaxTotal.Should().Be(control.TaxTotal);
    }

    // ── (2) EUR line converts at the resolved rate; TRY economics are correct; mixed EUR+TRY works ────
    [Fact]
    public async Task EurLine_ConvertsToTry_EconomicsInTry()
    {
        var fake = new FakeRateSource(new() { ["EUR"] = 42.60m });
        var resolver = new OfferFxResolver(fake);

        // 500 EUR @ 42.60 = 21_300 TRY  +  1_000 TRY native
        var eur = Line("EUR", 500m);
        var tryv = Line("TRY", 1000m);
        var offer = OfferWith(eur, tryv);

        await resolver.ResolveAndConvertAsync(offer, DateTimeOffset.UtcNow, default);
        Calc.Calculate(offer);

        eur.SourceUnitPrice.Should().Be(500m);      // source figure preserved
        eur.CurrencyCode.Should().Be("EUR");        // source currency kept
        eur.UnitPrice.Should().Be(21_300m);         // converted TRY the math uses
        eur.LineSubtotal.Should().Be(21_300m);

        tryv.SourceUnitPrice.Should().BeNull();      // native line untouched
        tryv.UnitPrice.Should().Be(1000m);

        offer.GrandTotal.Should().Be(22_300m);      // 21_300 + 1_000 (no tax)

        // One FX snapshot row for EUR only, frozen with the rate + UTC timestamps.
        var row = offer.FxSnapshots.Should().ContainSingle().Subject;
        row.SourceCurrencyCode.Should().Be("EUR");
        row.SettlementCurrencyCode.Should().Be("TRY");
        row.Rate.Should().Be(42.60m);
        row.RateDate.Kind.Should().Be(DateTimeKind.Utc);
        row.ResolvedAtUtc.Kind.Should().Be(DateTimeKind.Utc);
        fake.Calls.Should().Be(1, "only the one distinct non-TRY currency is resolved");
    }

    // ── (3) No effective rate → fail loud, no silent mispricing ───────────────────────────────────────
    [Fact]
    public async Task MissingRate_FailsLoud_WithSrCode()
    {
        var fake = new FakeRateSource(new() { ["EUR"] = null });   // HasRate = false
        var resolver = new OfferFxResolver(fake);
        var offer = OfferWith(Line("EUR", 500m));

        var act = async () => await resolver.ResolveAndConvertAsync(offer, DateTimeOffset.UtcNow, default);

        (await act.Should().ThrowAsync<AizenBusinessException>())
            .Which.Message.Should().Contain("SR_FX_RATE_UNAVAILABLE");
        offer.Items.Single().SourceUnitPrice.Should().BeNull("a failed resolve must not half-convert the line");
        offer.FxSnapshots.Should().BeEmpty();
    }

    // ── (5) Rounding matches S1: round(source × rate, 2, AwayFromZero) — NOT banker's rounding ─────────
    [Fact]
    public async Task Conversion_Rounding_IsAwayFromZero_MatchingS1()
    {
        // 1 × 0.125 = 0.125 → AwayFromZero ⇒ 0.13 (banker's/ToEven would give 0.12).
        var fake = new FakeRateSource(new() { ["EUR"] = 0.125m });
        var resolver = new OfferFxResolver(fake);
        var line = Line("EUR", 1m);
        var offer = OfferWith(line);

        await resolver.ResolveAndConvertAsync(offer, DateTimeOffset.UtcNow, default);

        line.UnitPrice.Should().Be(0.13m);
        line.SourceUnitPrice.Should().Be(1m);
    }

    // ── Re-submit re-resolves from the SOURCE figure (never double-converts) ──────────────────────────
    [Fact]
    public async Task ReSubmit_ConvertsFromSource_NotFromConvertedValue()
    {
        var resolver1 = new OfferFxResolver(new FakeRateSource(new() { ["EUR"] = 40m }));
        var eur = Line("EUR", 500m);
        var offer = OfferWith(eur);

        await resolver1.ResolveAndConvertAsync(offer, DateTimeOffset.UtcNow, default);
        eur.UnitPrice.Should().Be(20_000m);   // 500 × 40

        // A later re-submit at a new rate must convert 500 (source), not 20_000 (already converted).
        var resolver2 = new OfferFxResolver(new FakeRateSource(new() { ["EUR"] = 42m }));
        await resolver2.ResolveAndConvertAsync(offer, DateTimeOffset.UtcNow, default);
        eur.UnitPrice.Should().Be(21_000m);   // 500 × 42, NOT 20_000 × 42
        eur.SourceUnitPrice.Should().Be(500m);
    }
}
