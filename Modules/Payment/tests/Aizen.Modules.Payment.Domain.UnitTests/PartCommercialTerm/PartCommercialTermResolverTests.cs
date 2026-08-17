using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.PartCommercialTerm;
using FluentAssertions;

namespace Aizen.Modules.Payment.Domain.UnitTests.PartCommercialTerm;

/// <summary>
/// BE-S5b — the pure part commercial term resolver. Asserts the established Payment resolver contract: specificity ordering
/// (product &gt; provider &gt; brand &gt; category &gt; global), priority tie-break, candidacy exclusion, fail-loud conflict on a
/// (rank, priority) tie, and the create/update overlap guard. Effective-date filtering is the repository's job (the resolver
/// receives an already-active set), so it's asserted in the repository/handler layer.
/// </summary>
public sealed class PartCommercialTermResolverTests
{
    private static readonly DateTime From = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static PartCommercialTermEntity Term(
        long id = 1,
        string? brand = null, string? productCode = null, long? providerProfileId = null, string? categoryCode = null,
        string currency = "TRY", CommissionRulePriority priority = CommissionRulePriority.Standard, DateTime? to = null)
    {
        var t = PartCommercialTermEntity.Create(
            brand, productCode, providerProfileId, categoryCode, currency,
            supplierListPrice: 600m, providerDealerMargin: 120m,
            maxCustomerDiscount: 90m, supplierFundedAmount: 30m, providerFundedAmount: 40m, platformFundedAmount: 20m,
            minimumProviderReceivable: 500m, maximumDiscountableAmount: 90m,
            version: 1, priority: priority, effectiveFrom: From, effectiveTo: to, termCode: "T");
        t.Id = id;
        return t;
    }

    // ── Specificity: product(8) > provider(4) > brand(2) > category(1) > global(0) ──
    [Fact]
    public void Specificity_Ranks_Are_Ordered()
    {
        PartCommercialTermResolver.ComputeSpecificityRank(Term(productCode: "P1")).Should().Be(8);
        PartCommercialTermResolver.ComputeSpecificityRank(Term(providerProfileId: 7)).Should().Be(4);
        PartCommercialTermResolver.ComputeSpecificityRank(Term(brand: "YAMAHA")).Should().Be(2);
        PartCommercialTermResolver.ComputeSpecificityRank(Term(categoryCode: "MAINTENANCE")).Should().Be(1);
        PartCommercialTermResolver.ComputeSpecificityRank(Term()).Should().Be(0);
        // A product term outranks any combination of the lower dims.
        PartCommercialTermResolver.ComputeSpecificityRank(Term(providerProfileId: 7, brand: "YAMAHA", categoryCode: "MAINTENANCE")).Should().Be(7);
    }

    [Fact]
    public void Resolve_Picks_Most_Specific_Product()
    {
        var all = new[]
        {
            Term(id: 1),                                       // global
            Term(id: 2, categoryCode: "MAINTENANCE"),          // category
            Term(id: 3, providerProfileId: 7),                 // provider
            Term(id: 4, productCode: "P1"),                    // product (most specific)
        };
        var ctx = new PartCommercialTermResolveContext(
            ProductCode: "P1", ProviderProfileId: 7, CategoryCode: "MAINTENANCE", CurrencyCode: "TRY");

        PartCommercialTermResolver.Resolve(all, ctx)!.Id.Should().Be(4);
    }

    [Fact]
    public void Resolve_Falls_Back_To_Global_And_Provider_When_Product_Unknown()
    {
        // No product/brand in context → only provider/category/global terms are candidates; provider (rank 4) wins.
        var all = new[] { Term(id: 1), Term(id: 2, categoryCode: "MAINTENANCE"), Term(id: 3, providerProfileId: 7) };
        var ctx = new PartCommercialTermResolveContext(ProviderProfileId: 7, CategoryCode: "MAINTENANCE", CurrencyCode: "TRY");
        PartCommercialTermResolver.Resolve(all, ctx)!.Id.Should().Be(3);
    }

    // ── Candidacy ──
    [Fact]
    public void Currency_Mismatch_Excludes_Term()
        => PartCommercialTermResolver.IsCandidate(Term(currency: "EUR"),
            new PartCommercialTermResolveContext(CurrencyCode: "TRY")).Should().BeFalse();

    [Fact]
    public void Product_Scoped_Term_Excluded_When_Context_Has_No_Product()
        => PartCommercialTermResolver.IsCandidate(Term(productCode: "P1"),
            new PartCommercialTermResolveContext(CategoryCode: "MAINTENANCE", CurrencyCode: "TRY")).Should().BeFalse();

    [Fact]
    public void Resolve_Returns_Null_When_Nothing_Matches()
        => PartCommercialTermResolver.Resolve(
            new[] { Term(id: 1, categoryCode: "OTHER") },
            new PartCommercialTermResolveContext(CategoryCode: "MAINTENANCE", CurrencyCode: "TRY")).Should().BeNull();

    // ── Priority tie-break + fail-loud conflict ──
    [Fact]
    public void Resolve_Uses_Priority_As_TieBreak()
    {
        var low  = Term(id: 1, categoryCode: "MAINTENANCE", priority: CommissionRulePriority.Low);
        var high = Term(id: 2, categoryCode: "MAINTENANCE", priority: CommissionRulePriority.High);
        PartCommercialTermResolver.Resolve(new[] { low, high },
            new PartCommercialTermResolveContext(CategoryCode: "MAINTENANCE", CurrencyCode: "TRY"))!.Id.Should().Be(2);
    }

    [Fact]
    public void Resolve_Throws_Conflict_On_Ambiguous_Tie()
    {
        var a = Term(id: 1, categoryCode: "MAINTENANCE", priority: CommissionRulePriority.Standard);
        var b = Term(id: 2, categoryCode: "MAINTENANCE", priority: CommissionRulePriority.Standard);

        Action act = () => PartCommercialTermResolver.Resolve(new[] { a, b },
            new PartCommercialTermResolveContext(CategoryCode: "MAINTENANCE", CurrencyCode: "TRY"));

        act.Should().Throw<AizenBusinessException>()
           .Where(e => e.ErrorCode == (int)PaymentErrorCode.PartCommercialTermConflict);
    }

    // ── Create/update overlap guard ──
    [Fact]
    public void FindOverlappingConflict_Detects_Same_Scope_Priority_Overlap()
    {
        var existing  = Term(id: 1, categoryCode: "MAINTENANCE");
        var candidate = Term(id: 0, categoryCode: "MAINTENANCE");
        PartCommercialTermResolver.FindOverlappingConflict(candidate, new[] { existing }).Should().NotBeNull();
    }

    [Fact]
    public void FindOverlappingConflict_Ignores_Different_Scope()
    {
        var existing  = Term(id: 1, categoryCode: "MAINTENANCE");
        var candidate = Term(id: 0, categoryCode: "REPAIR");
        PartCommercialTermResolver.FindOverlappingConflict(candidate, new[] { existing }).Should().BeNull();
    }

    [Fact]
    public void FindOverlappingConflict_Ignores_NonOverlapping_Windows()
    {
        var existing  = Term(id: 1, categoryCode: "MAINTENANCE", to: new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        // Candidate starts exactly when existing ends → half-open [from,to) do not overlap.
        var candidate = PartCommercialTermEntity.Create(
            null, null, null, "MAINTENANCE", "TRY", 600m, 120m, 90m, 30m, 40m, 20m, 500m, 90m,
            version: 2, priority: CommissionRulePriority.Standard,
            effectiveFrom: new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc), effectiveTo: null, termCode: "T2");
        candidate.Id = 0;
        PartCommercialTermResolver.FindOverlappingConflict(candidate, new[] { existing }).Should().BeNull();
    }
}
