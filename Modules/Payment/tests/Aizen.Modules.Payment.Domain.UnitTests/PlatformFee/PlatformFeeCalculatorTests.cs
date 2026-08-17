using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.PlatformFee;
using FluentAssertions;

namespace Aizen.Modules.Payment.Domain.UnitTests.PlatformFee;

public sealed class PlatformFeeCalculatorTests
{
    private static PlatformFeeResolution Res(
        PlatformFeeModel model,
        decimal? rate = null, decimal? min = null, decimal? max = null,
        decimal? fixedAmount = null, decimal? vatRate = null)
        => new(RuleId: 1, RuleCode: "PFR", Model: model, Rate: rate, MinAmount: min, MaxAmount: max,
               FixedAmount: fixedAmount, VatRate: vatRate, SpecificityRank: 1,
               Priority: CommissionRulePriority.Standard, Source: "Global");

    // ── 4 models ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Percentage_Net_Is_Rounded_Base_Times_Rate()
    {
        var net = PlatformFeeCalculator.ApplyNet(1000m, Res(PlatformFeeModel.Percentage, rate: 0.025m));
        net.Should().Be(25.00m);
    }

    [Fact]
    public void Fixed_Net_Is_The_Fixed_Amount()
    {
        var net = PlatformFeeCalculator.ApplyNet(1000m, Res(PlatformFeeModel.Fixed, fixedAmount: 50m));
        net.Should().Be(50.00m);
    }

    [Fact]
    public void Waived_Net_Is_Zero()
    {
        var net = PlatformFeeCalculator.ApplyNet(1000m, Res(PlatformFeeModel.Waived));
        net.Should().Be(0m);
    }

    // ── PercentageWithBounds clamp boundaries ────────────────────────────────────

    [Fact]
    public void Bounds_Below_Min_Clamps_To_Min()
    {
        // 1000 × 2.5% = 25 < min 99 → 99
        var net = PlatformFeeCalculator.ApplyNet(1000m,
            Res(PlatformFeeModel.PercentageWithBounds, rate: 0.025m, min: 99m, max: 1500m));
        net.Should().Be(99m);
    }

    [Fact]
    public void Bounds_Above_Max_Clamps_To_Max()
    {
        // 100000 × 2.5% = 2500 > max 1500 → 1500
        var net = PlatformFeeCalculator.ApplyNet(100000m,
            Res(PlatformFeeModel.PercentageWithBounds, rate: 0.025m, min: 99m, max: 1500m));
        net.Should().Be(1500m);
    }

    [Fact]
    public void Bounds_Within_Range_Returns_Computed()
    {
        // 40000 × 2.5% = 1000, within [99,1500] → 1000
        var net = PlatformFeeCalculator.ApplyNet(40000m,
            Res(PlatformFeeModel.PercentageWithBounds, rate: 0.025m, min: 99m, max: 1500m));
        net.Should().Be(1000m);
    }

    [Theory]
    [InlineData(3960, 99)]     // 3960 × 2.5% = 99.00 exactly = min → min
    [InlineData(60000, 1500)]  // 60000 × 2.5% = 1500.00 exactly = max → max
    public void Bounds_Exact_Boundaries_Are_Kept(int baseAmount, int expected)
    {
        var net = PlatformFeeCalculator.ApplyNet(baseAmount,
            Res(PlatformFeeModel.PercentageWithBounds, rate: 0.025m, min: 99m, max: 1500m));
        net.Should().Be(expected);
    }

    // ── VAT net/vat/gross (§13.3) ────────────────────────────────────────────────

    [Fact]
    public void Breakdown_Gross_Equals_Net_Plus_Vat_And_Vat_Is_Rounded_Net_Times_Rate()
    {
        var b = PlatformFeeCalculator.ComputeBreakdown(
            4000m, Res(PlatformFeeModel.Percentage, rate: 0.025m), vatRate: 0.20m, vatSource: "Default");

        b.Net.Should().Be(100.00m);          // 4000 × 2.5%
        b.Vat.Should().Be(20.00m);           // 100 × 20%
        b.Gross.Should().Be(120.00m);        // net + vat
        b.Gross.Should().Be(b.Net + b.Vat);
        b.VatSource.Should().Be("Default");
    }

    [Fact]
    public void Percentage_Missing_Rate_Throws_Invalid()
    {
        Action act = () => PlatformFeeCalculator.ApplyNet(1000m, Res(PlatformFeeModel.Percentage, rate: null));
        act.Should().Throw<AizenBusinessException>()
           .Where(e => e.ErrorCode == (int)PaymentErrorCode.PlatformFeeRuleInvalid);
    }
}
