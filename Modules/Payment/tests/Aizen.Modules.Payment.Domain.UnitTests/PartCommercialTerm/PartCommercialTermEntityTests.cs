using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.PartCommercialTerm;
using FluentAssertions;

namespace Aizen.Modules.Payment.Domain.UnitTests.PartCommercialTerm;

/// <summary>
/// BE-S5a — the validating factory. Fail-loud (<see cref="PaymentErrorCode.PartCommercialTermInvalid"/>): all money ≥ 0,
/// Σ(funded) ≤ maximumDiscountableAmount, maxCustomerDiscount ≤ maximumDiscountableAmount, EffectiveTo &gt; EffectiveFrom.
/// </summary>
public sealed class PartCommercialTermEntityTests
{
    private static readonly DateTime From = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static PartCommercialTermEntity Create(
        decimal supplierListPrice = 600m, decimal providerDealerMargin = 120m,
        decimal maxCustomerDiscount = 90m, decimal supplierFunded = 30m, decimal providerFunded = 40m, decimal platformFunded = 20m,
        decimal minimumProviderReceivable = 500m, decimal maximumDiscountableAmount = 90m,
        DateTime? to = null)
        => PartCommercialTermEntity.Create(
            brand: null, productCode: null, providerProfileId: null, categoryCode: "MAINTENANCE", currencyCode: "try",
            supplierListPrice, providerDealerMargin, maxCustomerDiscount, supplierFunded, providerFunded, platformFunded,
            minimumProviderReceivable, maximumDiscountableAmount,
            version: 1, priority: CommissionRulePriority.Standard, effectiveFrom: From, effectiveTo: to, termCode: "T");

    [Fact]
    public void Valid_Term_Is_Created_And_Normalizes_Currency()
    {
        var t = Create();
        t.CurrencyCode.Should().Be("TRY");
        t.CategoryCode.Should().Be("MAINTENANCE");
        t.Status.Should().Be(CommissionRuleStatus.Active);
    }

    [Theory]
    [InlineData(-1, 0, 0, 0, 0, 0)]   // supplierListPrice < 0
    [InlineData(0, -1, 0, 0, 0, 0)]   // providerDealerMargin < 0
    [InlineData(0, 0, -1, 0, 0, 0)]   // maxCustomerDiscount < 0
    [InlineData(0, 0, 0, -1, 0, 0)]   // supplierFunded < 0
    public void Negative_Money_Throws(decimal listPrice, decimal margin, decimal maxDisc, decimal supp, decimal prov, decimal plat)
    {
        Action act = () => Create(
            supplierListPrice: listPrice, providerDealerMargin: margin,
            maxCustomerDiscount: maxDisc, supplierFunded: supp, providerFunded: prov, platformFunded: plat,
            maximumDiscountableAmount: 100m, minimumProviderReceivable: 0m);
        act.Should().Throw<AizenBusinessException>().Where(e => e.ErrorCode == (int)PaymentErrorCode.PartCommercialTermInvalid);
    }

    [Fact]
    public void FundedSum_Exceeding_MaxDiscountable_Throws()
    {
        // 30 + 40 + 25 = 95 > maxDiscountable 90.
        Action act = () => Create(supplierFunded: 30m, providerFunded: 40m, platformFunded: 25m, maximumDiscountableAmount: 90m);
        act.Should().Throw<AizenBusinessException>()
           .Where(e => e.ErrorCode == (int)PaymentErrorCode.PartCommercialTermInvalid)
           .WithMessage("*MaximumDiscountableAmount*");
    }

    [Fact]
    public void MaxCustomerDiscount_Exceeding_MaxDiscountable_Throws()
    {
        Action act = () => Create(maxCustomerDiscount: 120m, maximumDiscountableAmount: 90m);
        act.Should().Throw<AizenBusinessException>()
           .Where(e => e.ErrorCode == (int)PaymentErrorCode.PartCommercialTermInvalid);
    }

    [Fact]
    public void EffectiveTo_Not_After_From_Throws()
    {
        Action act = () => Create(to: From);   // to == from
        act.Should().Throw<AizenBusinessException>()
           .Where(e => e.ErrorCode == (int)PaymentErrorCode.PartCommercialTermInvalid);
    }

    [Fact]
    public void Deactivate_Then_Reactivate_Toggles_Status()
    {
        var t = Create();
        t.Deactivate();
        t.IsActive.Should().BeFalse();
        t.Status.Should().Be(CommissionRuleStatus.Inactive);
        t.Reactivate();
        t.IsActive.Should().BeTrue();
        t.Status.Should().Be(CommissionRuleStatus.Active);
    }
}
