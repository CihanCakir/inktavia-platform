using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Abstraction.Interface.Service;
using Aizen.Modules.CargoDry.Application.Services;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using FluentAssertions;
using NSubstitute;

namespace Aizen.Modules.CargoDry.Application.UnitTests;

/// <summary>
/// Split-host regression guard (Supply v2, Sep 2026): aizen-cargodry runs WITHOUT Payment.Application,
/// so the real <see cref="ICargoDryCommissionRuleLookupService"/> (Payment-backed) is absent and the
/// <see cref="NullCargoDryCommissionRuleLookupService"/> null-object is registered instead. Both provider-
/// and product-channel lookups return null, which must degrade the resolver exactly to its lower tiers.
///
/// This proves a consignment attribution still resolves to the agreement rate (Tier 4) in that host —
/// i.e. attribution rows get a real salePrice/commission rather than dying in Autofac / going Unresolved.
/// </summary>
public sealed class CargoDryCommercialRuleResolverNullLookupTests
{
    private static CargoDryCommercialRuleResolver BuildResolver(
        ICargoDryProductRepository productRepo,
        ICargoDryConsignmentAgreementRepository agreementRepo)
        // Real null-object — the exact instance TryAddScoped registers when Payment DI is absent.
        => new(productRepo, agreementRepo, new NullCargoDryCommissionRuleLookupService());

    [Fact]
    public async Task ConsignmentSellThrough_WithoutPaymentDi_ResolvesToAgreementRate()
    {
        // ── Arrange ────────────────────────────────────────────────────────────────
        const long   agreementId = 4;
        const decimal agreementRate = 0.20m;   // 20% consignment rate
        const decimal salePrice = 149.99m;

        var agreement = CargoDryConsignmentAgreementEntity.Create(
            agreementCode:           "CDA-TEST-0004",
            providerProfileId:       100011,
            productCode:             "CD-MARINE-PRO",
            consignmentRate:         agreementRate,
            minimumSettlementAmount: 0m,
            currencyCode:            "TRY",
            maxKitCount:             100,
            startDateUtc:            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var agreementRepo = Substitute.For<ICargoDryConsignmentAgreementRepository>();
        agreementRepo.GetByIdAsync(agreementId, Arg.Any<CancellationToken>())
                     .Returns(agreement);

        // Product repo must never be reached — Tier 4 short-circuits before the Tier 5 product default.
        var productRepo = Substitute.For<ICargoDryProductRepository>();

        var resolver = BuildResolver(productRepo, agreementRepo);

        var request = new CargoDryCommercialRuleResolutionRequest
        {
            ProviderProfileId      = 100011,
            ProductCode            = "CD-MARINE-PRO",
            SalesChannel           = SalesChannel.ConsignmentSellThrough,
            CommercialModel        = CargoDryCommercialModel.PrincipalSale,
            CurrencyCode           = "TRY",
            EffectiveAtUtc         = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc),
            SalePrice              = salePrice,
            ConsignmentAgreementId = agreementId,
        };

        // ── Act ────────────────────────────────────────────────────────────────────
        var result = await resolver.ResolveAsync(request, CancellationToken.None);

        // ── Assert ─────────────────────────────────────────────────────────────────
        result.CanResolve.Should().BeTrue();
        result.RuleSource.Should().Be("ConsignmentAgreement");
        result.ResolvedRate.Should().Be(agreementRate);
        result.ProviderShareAmount.Should().Be(29.998m);   // 149.99 × 0.20
        result.PlatformShareAmount.Should().Be(119.992m);  // 149.99 − 29.998

        // Null lookup was consulted (and returned null) for both commission-rule tiers before falling to Tier 4.
        await productRepo.DidNotReceive().GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConsignmentSellThrough_ZeroRateAgreement_FallsThroughToProductDefault()
    {
        // Guards the fall-through: a zero-rate agreement must NOT resolve at Tier 4; with the null lookup
        // the resolver drops to the Tier 5 product default (proving the tiers below Payment still function).
        const long agreementId = 7;

        var zeroRateAgreement = CargoDryConsignmentAgreementEntity.Create(
            agreementCode:           "CDA-TEST-0007",
            providerProfileId:       100011,
            productCode:             "CD-MARINE-PRO",
            consignmentRate:         0m,
            minimumSettlementAmount: 0m,
            currencyCode:            "TRY",
            maxKitCount:             100,
            startDateUtc:            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var agreementRepo = Substitute.For<ICargoDryConsignmentAgreementRepository>();
        agreementRepo.GetByIdAsync(agreementId, Arg.Any<CancellationToken>())
                     .Returns(zeroRateAgreement);

        var product = CargoDryProductEntity.Create(
            productCode:            "CD-MARINE-PRO",
            name:                   "CargoDry Marine Pro",
            description:            "Test product",
            validityDays:           365,
            retailPrice:            149.99m,
            currencyCode:           "TRY",
            providerCommissionRate: 0.15m);

        var productRepo = Substitute.For<ICargoDryProductRepository>();
        productRepo.GetByCodeAsync("CD-MARINE-PRO", Arg.Any<CancellationToken>())
                   .Returns(product);

        var resolver = BuildResolver(productRepo, agreementRepo);

        var result = await resolver.ResolveAsync(new CargoDryCommercialRuleResolutionRequest
        {
            ProviderProfileId      = 100011,
            ProductCode            = "CD-MARINE-PRO",
            SalesChannel           = SalesChannel.ConsignmentSellThrough,
            CommercialModel        = CargoDryCommercialModel.PrincipalSale,
            CurrencyCode           = "TRY",
            EffectiveAtUtc         = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc),
            SalePrice              = 149.99m,
            ConsignmentAgreementId = agreementId,
        }, CancellationToken.None);

        result.CanResolve.Should().BeTrue();
        result.RuleSource.Should().Be("CargoDryProductDefault");
        result.ResolvedRate.Should().Be(0.15m);
        result.Warnings.Should().Contain(w => w.Contains("ConsignmentRate=0"));
    }
}
