using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Economics;
using FluentAssertions;

namespace Aizen.Modules.Payment.Domain.UnitTests.Economics;

/// <summary>
/// S2d — the immutable per-line pricing attribute snapshot (§20.6). Captured at acceptance as a child of the line
/// economics snapshot; descriptive metadata that must NOT move the money math or the 8 equalities. Verifies the
/// self-contained Lookup capture (item code + denormalized label), the validating factory (tamper → throw), and that
/// attaching attributes leaves the aggregate economics byte-identical.
/// </summary>
public sealed class OfferLineAttributeSnapshotTests
{
    private const int LookupType = 1;   // raw SR PricingAttributeDataType.Lookup

    private static LineAttributeSnapshotInput Paint(string code = "ANTIFOULING", string label = "Zehirli Boya (Antifouling)")
        => new("PAINT_TYPE", LookupType, code, label, null, null, null, SortOrder: 0);

    private static LineEconomicsInput ServiceLine(IReadOnlyList<LineAttributeSnapshotInput>? attrs = null)
        => new("L-SERVICE", ItemType: 1, PricingMethod: 1,
            GrossBeforeDiscount: 5000m, CustomerDiscount: 0m, ProviderFundedDiscount: 0m, PlatformFundedDiscount: 0m,
            CommissionEligibility: LineCommissionEligibility.Eligible,
            CommissionBase: 5000m, CommissionRate: 0.12m, CommissionAmount: 600m,
            ProviderNet: 4400m, LineVat: 0m, LineTotal: 5000m,
            RuleId: 7, RuleCode: "PLAN-STD", Commissionable: true, SortOrder: 0, Attributes: attrs);

    private static LineEconomicsInput TravelExempt()
        => new("L-TRAVEL", 10, 1, 800m, 0m, 0m, 0m, LineCommissionEligibility.Exempt,
            0m, 0m, 0m, 800m, 0m, 800m, null, null, false, 1);

    private static PlatformFeeInput Fee() => new(3, 0.025m, 99m, 1500m, 5800m, 145m, 29m, 174m);

    // ── Factory: self-contained Lookup capture ───────────────────────────────────
    [Fact]
    public void Create_Lookup_CapturesItemCodeAndLabel()
    {
        var s = OfferLineAttributeSnapshotEntity.Create(
            "paint_type", LookupType, "antifouling", "Zehirli Boya (Antifouling)", null, null, null, 3);

        s.DefinitionCode.Should().Be("PAINT_TYPE");             // normalized upper
        s.ValueLookupItemCode.Should().Be("ANTIFOULING");        // normalized upper
        s.ValueLookupItemLabel.Should().Be("Zehirli Boya (Antifouling)");  // denormalized — no re-lookup needed
        s.DataType.Should().Be(LookupType);
        s.SortOrder.Should().Be(3);
    }

    [Fact]
    public void Create_EmptyDefinitionCode_Throws()
    {
        var act = () => OfferLineAttributeSnapshotEntity.Create(
            "  ", LookupType, "ANTIFOULING", "x", null, null, null, 0);
        act.Should().Throw<PaymentEconomicsInvariantException>().WithMessage("*DefinitionCode*");
    }

    // ── CreateFromLines attaches the attribute children to the right line ─────────
    [Fact]
    public void CreateFromLines_AttachesAttributeSnapshots_ToTheLine()
    {
        var s = PaymentEconomicsSnapshotEntity.CreateFromLines(
            contextId: 42, currencyCode: "TRY",
            lines: new[] { ServiceLine(new[] { Paint() }), TravelExempt() },
            platformFee: Fee(), customerPayableServiceAmount: 5800m);

        var serviceLine = s.OfferLines.Single(l => l.LineRef == "L-SERVICE");
        serviceLine.AttributeSnapshots.Should().HaveCount(1);
        var attr = serviceLine.AttributeSnapshots.Single();
        attr.DefinitionCode.Should().Be("PAINT_TYPE");
        attr.ValueLookupItemCode.Should().Be("ANTIFOULING");
        attr.ValueLookupItemLabel.Should().Be("Zehirli Boya (Antifouling)");

        // A line with no attributes has none.
        s.OfferLines.Single(l => l.LineRef == "L-TRAVEL").AttributeSnapshots.Should().BeEmpty();
    }

    // ── Attributes are descriptive: the 8-equality economics are unchanged ────────
    [Fact]
    public void Attributes_DoNotChange_TheAggregateEconomics()
    {
        var withAttrs = PaymentEconomicsSnapshotEntity.CreateFromLines(
            42, "TRY", new[] { ServiceLine(new[] { Paint() }), TravelExempt() }, Fee(), 5800m);
        var without = PaymentEconomicsSnapshotEntity.CreateFromLines(
            42, "TRY", new[] { ServiceLine(), TravelExempt() }, Fee(), 5800m);

        withAttrs.CommissionAmountSnapshot.Should().Be(without.CommissionAmountSnapshot);
        withAttrs.ProviderNetAmountSnapshot.Should().Be(without.ProviderNetAmountSnapshot);
        withAttrs.CustomerTotalAmountSnapshot.Should().Be(without.CustomerTotalAmountSnapshot);
        withAttrs.PlatformGrossShareSnapshot.Should().Be(without.PlatformGrossShareSnapshot);
        withAttrs.ServiceGrossAmountSnapshot.Should().Be(without.ServiceGrossAmountSnapshot);

        // Sanity: the canonical smoke values still hold with attributes present.
        withAttrs.CustomerTotalAmountSnapshot.Should().Be(5974m);
        withAttrs.ProviderNetAmountSnapshot.Should().Be(5200m);
    }
}
