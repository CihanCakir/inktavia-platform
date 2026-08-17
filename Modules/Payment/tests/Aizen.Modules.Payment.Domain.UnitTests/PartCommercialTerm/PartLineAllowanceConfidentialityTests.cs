using System.Reflection;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;
using FluentAssertions;

namespace Aizen.Modules.Payment.Domain.UnitTests.PartCommercialTerm;

/// <summary>
/// BE-S5 HEADLINE — cost confidentiality (§20.9). The confidential cost fields (<c>supplierListPrice</c>,
/// <c>providerDealerMargin</c>, and any raw cost) must never leave the Payment module. The ONLY projection that crosses the
/// boundary is the cost-free part-line allowance. These tests reflect over every boundary-crossing DTO (the remote-call
/// request + response, the per-line allowance, and the funding split) and FAIL if any cost/margin field is present — so the
/// test breaks the moment someone adds <c>supplierListPrice</c> (or any cost) to the projection.
/// </summary>
public sealed class PartLineAllowanceConfidentialityTests
{
    // Forbidden substrings (case-insensitive) — any of these in a boundary DTO property name reveals cost.
    private static readonly string[] Forbidden =
        { "supplierlistprice", "providerdealermargin", "dealermargin", "listprice", "cost", "margin" };

    private static readonly Type[] BoundaryTypes =
    {
        typeof(ResolvePartLineAllowancesRemoteCallResponse),
        typeof(PartLineAllowanceDto),
        typeof(PartFundingSplitDto),
        typeof(ResolvePartLineAllowancesRemoteCallRequest),
        typeof(ResolvePartLineInputDto),
    };

    [Fact]
    public void No_Boundary_Dto_Exposes_A_Cost_Or_Margin_Field()
    {
        foreach (var type in BoundaryTypes)
        {
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var p in props)
            {
                var name = p.Name.ToLowerInvariant();
                Forbidden.Any(f => name.Contains(f))
                    .Should().BeFalse($"{type.Name}.{p.Name} would leak part cost/margin across the Payment boundary");
            }
        }
    }

    [Fact]
    public void Allowance_Exposes_Only_The_Cost_Free_Caps()
    {
        var names = typeof(PartLineAllowanceDto)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name).ToHashSet();

        // The derived, cost-free caps SR (and later S9) may act on.
        names.Should().Contain("MaxAllowedCustomerDiscount");
        names.Should().Contain("MinimumProviderReceivable");
        names.Should().Contain("Funding");

        // The funded split carries amounts only — no cost.
        var funding = typeof(PartFundingSplitDto)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name).ToHashSet();
        funding.Should().BeEquivalentTo(new[] { "SupplierFundedAmount", "ProviderFundedAmount", "PlatformFundedAmount" });
    }
}
